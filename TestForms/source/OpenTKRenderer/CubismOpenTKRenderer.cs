using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics.ES20;

namespace CubismFramework
{
    public class CubismOpenTKRenderer : ICubismRenderer, IDisposable
    {
        private const int ClippingMaskSize = 256;
        private Matrix4 MvpMatrix;
        private float[] ModelColor = new float[4];
        private static float[] DefaultModelColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
        private CubismOpenTKState State = new CubismOpenTKState();
        private CubismOpenTKShaderManager ShaderManager = new CubismOpenTKShaderManager();
        private List<CubismOpenTKClippingMask> ClippingMasks = new List<CubismOpenTKClippingMask>();
        private List<CubismOpenTKTexture> Textures = new List<CubismOpenTKTexture>();
        private bool disposedValue = false;

        private BlendModeType BlendMode
        {
            set
            {
                switch (value)
                {
                case BlendModeType.Normal:
                    GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                    break;

                case BlendModeType.Add:
                    GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.One, BlendingFactorSrc.Zero, BlendingFactorDest.One);
                    break;

                case BlendModeType.Multiply:
                    GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.Zero, BlendingFactorDest.One);
                    break;
                }
            }
        }

        private bool UseCulling
        {
            set
            {
                if (value)
                    GL.Enable(EnableCap.CullFace);
                else
                    GL.Disable(EnableCap.CullFace);
            }
        }

        public bool UsePremultipliedAlpha { set; get; }

        ~CubismOpenTKRenderer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var clipping_mask in ClippingMasks)
                {
                    clipping_mask.Dispose();
                }
                foreach (var texture in Textures)
                {
                    texture.Dispose();
                }
                ShaderManager.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public ICubismClippingMask CreateClippingMask()
        {
            var clipping_mask = new CubismOpenTKClippingMask(ClippingMaskSize, ClippingMaskSize);
            ClippingMasks.Add(clipping_mask);
            return clipping_mask;
        }

        public ICubismTexture CreateTexture(byte[] texture_bytes)
        {
            var bitmap = new System.Drawing.Bitmap(new MemoryStream(texture_bytes));
            var texture = new CubismOpenTKTexture(bitmap);
            Textures.Add(texture);
            return texture;
        }

        public void DisposeClippingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismOpenTKClippingMask)iclipping_mask;
            clipping_mask.Dispose();
            ClippingMasks.Remove(clipping_mask);
        }

        public void DisposeTexture(ICubismTexture itexture)
        {
            var texture = (CubismOpenTKTexture)itexture;
            texture.Dispose();
            Textures.Remove(texture);
        }

        public void StartDrawingModel(float[] model_color, Matrix4 mvp_matrix)
        {
            State.SaveState();

            GL.FrontFace(FrontFaceDirection.Ccw);
            
            GL.Disable(EnableCap.ScissorTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.ColorMask(true, true, true, true);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            if ((model_color != null) && (model_color.Length == 4))
                model_color.CopyTo(ModelColor, 0);
            else
                DefaultModelColor.CopyTo(ModelColor, 0);

            MvpMatrix = mvp_matrix;
        }

        public void StartDrawingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismOpenTKClippingMask)iclipping_mask;
            GL.Viewport(0, 0, clipping_mask.Width, clipping_mask.Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, clipping_mask.FrameBufferId);

            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var shader = ShaderManager.ShaderForDrawMask();
            GL.UseProgram(shader.ProgramId);

            GL.BlendFuncSeparate(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha);
        }

        unsafe public void DrawMask(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix4 clipping_matrix, bool use_culling)
        {
            var texture = (CubismOpenTKTexture)itexture;
            var clipping_mask = (CubismOpenTKClippingMask)iclipping_mask;

            UseCulling = use_culling;
            
            var shader = ShaderManager.ShaderForDrawMask();
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture.TextureId);
            GL.Uniform1(shader.SamplerTexture0Location, 0);

            GL.EnableVertexAttribArray((uint)shader.AttributePositionLocation);
            fixed (float* ptr = vertex_buffer)
                GL.VertexAttribPointer((uint)shader.AttributePositionLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, (IntPtr)ptr);

            GL.EnableVertexAttribArray((uint)shader.AttributeTexCoordLocation);
            fixed (float* ptr = uv_buffer)
                GL.VertexAttribPointer((uint)shader.AttributeTexCoordLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, (IntPtr)ptr);

            GL.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            GL.UniformMatrix4(shader.UniformClipMatrixLocation, false, ref clipping_matrix);
            GL.Uniform4(shader.UniformBaseColorLocation, -1.0f, -1.0f, 1.0f, 1.0f);

            fixed (short* ptr = index_buffer)
                GL.DrawElements(PrimitiveType.Triangles, index_buffer.Length, DrawElementsType.UnsignedShort, (IntPtr)ptr);
        }

        public void EndDrawingMask(ICubismClippingMask iclipping_mask)
        {
            State.RestoreFrameBuffer();
            State.RestoreViewport();
        }

        unsafe public void DrawMesh(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix4 clipping_matrix, BlendModeType blend_mode, bool use_culling, double opacity)
        {
            var texture = (CubismOpenTKTexture)itexture;
            var clipping_mask = iclipping_mask as CubismOpenTKClippingMask;
            bool use_clipping_mask = (clipping_mask != null);

            UseCulling = use_culling;
            BlendMode = blend_mode;

            var shader = ShaderManager.ShaderForDrawMesh(use_clipping_mask, UsePremultipliedAlpha);
            GL.UseProgram(shader.ProgramId);

            GL.EnableVertexAttribArray((uint)shader.AttributePositionLocation);
            fixed (float* ptr = vertex_buffer)
                GL.VertexAttribPointer((uint)shader.AttributePositionLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, (IntPtr)ptr);

            GL.EnableVertexAttribArray((uint)shader.AttributeTexCoordLocation);
            fixed (float* ptr = uv_buffer)
                GL.VertexAttribPointer((uint)shader.AttributeTexCoordLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, (IntPtr)ptr);

            if (use_clipping_mask == true)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, clipping_mask.TextureId);
                GL.Uniform1(shader.SamplerTexture1Location, 1);

                GL.UniformMatrix4(shader.UniformClipMatrixLocation, false, ref clipping_matrix);

                GL.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            }
            else
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture.TextureId);
            GL.Uniform1(shader.SamplerTexture0Location, 0);

            GL.UniformMatrix4(shader.UniformMatrixLocation, false, ref MvpMatrix);

            float[] color = new float[4];
            ModelColor.CopyTo(color, 0);
            color[3] *= (float)opacity;
            if (UsePremultipliedAlpha == true)
            {
                color[0] *= color[3];
                color[1] *= color[3];
                color[2] *= color[3];
            }
            GL.Uniform4(shader.UniformBaseColorLocation, color[0], color[1], color[2], color[3]);

            fixed (short* ptr = index_buffer)
                GL.DrawElements(PrimitiveType.Triangles, index_buffer.Length, DrawElementsType.UnsignedShort, (IntPtr)ptr);
        }

        public void EndDrawingModel()
        {
            State.RestoreState();
        }
    }
}