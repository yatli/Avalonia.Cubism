using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

using CubismFramework;

namespace Avalonia.Cubism.Render
{
    public class CubismAvaloniaRenderer : ICubismRenderer, IDisposable
    {
        private const int ClippingMaskSize = 256;
        private readonly GlInterface GL;
        private Matrix4x4 MvpMatrix;
        private float[] ModelColor = new float[4];
        private static float[] DefaultModelColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
        private CubismAvaloniaState State;
        private CubismAvaloniaShaderManager ShaderManager;
        private List<CubismAvaloniaClippingMask> ClippingMasks = new List<CubismAvaloniaClippingMask>();
        private List<CubismAvaloniaTexture> Textures = new List<CubismAvaloniaTexture>();
        private bool disposedValue = false;

        private BlendModeType BlendMode
        {
            set
            {
                switch (value)
                {
                case BlendModeType.Normal:
                    GL.BlendFuncSeparate(GL_ONE, GL_ONE_MINUS_SRC_ALPHA, BlendingFactorSrc.One, GL_ONE_MINUS_SRC_ALPHA);
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
                    GL.Enable(GL_CULL_FACE);
                else
                    GL.Disable(GL_CULL_FACE);
            }
        }

        public bool UsePremultipliedAlpha { set; get; }

        public CubismAvaloniaRenderer(GlInterface gl)
        {
            GL = gl;
            State = new CubismAvaloniaState(gl);
            ShaderManager = new CubismAvaloniaShaderManager(gl);
        }

        ~CubismAvaloniaRenderer()
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
            var clipping_mask = new CubismAvaloniaClippingMask(GL, ClippingMaskSize, ClippingMaskSize);
            ClippingMasks.Add(clipping_mask);
            return clipping_mask;
        }

        public ICubismTexture CreateTexture(byte[] texture_bytes)
        {
            var bitmap = new Bitmap(new MemoryStream(texture_bytes));
            var texture = new CubismAvaloniaTexture(GL, bitmap);
            Textures.Add(texture);
            return texture;
        }

        public void DisposeClippingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismAvaloniaClippingMask)iclipping_mask;
            clipping_mask.Dispose();
            ClippingMasks.Remove(clipping_mask);
        }

        public void DisposeTexture(ICubismTexture itexture)
        {
            var texture = (CubismAvaloniaTexture)itexture;
            texture.Dispose();
            Textures.Remove(texture);
        }

        public void StartDrawingModel(float[] model_color, Matrix4x4 mvp_matrix)
        {
            State.SaveState();

            GL.FrontFace(GL_CCW);
            
            GL.Disable(GL_SCISSOR_TEST);
            GL.Disable(GL_STENCIL_TEST);
            GL.Disable(GL_DEPTH_TEST);
            GL.Enable(GL_BLEND);
            GL.ColorMask(true, true, true, true);

            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
            GL.BindBuffer(GL_ARRAY_BUFFER, 0);

            if ((model_color != null) && (model_color.Length == 4))
                model_color.CopyTo(ModelColor, 0);
            else
                DefaultModelColor.CopyTo(ModelColor, 0);

            MvpMatrix = mvp_matrix;
        }

        public void StartDrawingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismAvaloniaClippingMask)iclipping_mask;
            GL.Viewport(0, 0, clipping_mask.Width, clipping_mask.Height);
            GL.BindFramebuffer(GL_FRAMEBUFFER, clipping_mask.FrameBufferId);

            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Clear(GL_COLOR_BUFFER_BIT);

            var shader = ShaderManager.ShaderForDrawMask();
            GL.UseProgram(shader.ProgramId);

            GL.BlendFuncSeparate(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha);
        }

        unsafe public void DrawMask(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix4x4 clipping_matrix, bool use_culling, bool is_inverted_mask)
        {
            var texture = (CubismAvaloniaTexture)itexture;
            var clipping_mask = (CubismAvaloniaClippingMask)iclipping_mask;

            UseCulling = use_culling;

            var shader = ShaderManager.ShaderForDrawMask();

            GL.ActiveTexture(GL_TEXTURE0);
            GL.BindTexture(GL_TEXTURE_2D, texture.TextureId);
            GL.Uniform1f(shader.SamplerTexture0Location, 0);

            GL.EnableVertexAttribArray(shader.AttributePositionLocation);
            fixed (float* ptr = vertex_buffer)
                GL.VertexAttribPointer(shader.AttributePositionLocation, 2, GL_FLOAT, 0, sizeof(float) * 2, (IntPtr)ptr);

            GL.EnableVertexAttribArray(shader.AttributeTexCoordLocation);
            fixed (float* ptr = uv_buffer)
                GL.VertexAttribPointer(shader.AttributeTexCoordLocation, 2, GL_FLOAT, 0, sizeof(float) * 2, (IntPtr)ptr);

            GL.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            GL.UniformMatrix4(shader.UniformClipMatrixLocation, false, ref clipping_matrix);
            GL.Uniform4(shader.UniformBaseColorLocation, -1.0f, -1.0f, 1.0f, 1.0f);

            fixed (short* ptr = index_buffer)
                GL.DrawElements(GL_TRIANGLES, index_buffer.Length, GL_UNSIGNED_SHORT, (IntPtr)ptr);
        }

        public void EndDrawingMask(ICubismClippingMask iclipping_mask)
        {
            State.RestoreFrameBuffer();
            State.RestoreViewport();
        }

        unsafe public void DrawMesh(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix4x4 clipping_matrix, BlendModeType blend_mode, bool use_culling, bool is_inverted_mask, double opacity)
        {
            var texture = (CubismAvaloniaTexture)itexture;
            var clipping_mask = iclipping_mask as CubismAvaloniaClippingMask;
            bool use_clipping_mask = (clipping_mask != null);

            UseCulling = use_culling;
            BlendMode = blend_mode;

            var shader = ShaderManager.ShaderForDrawMesh(use_clipping_mask, UsePremultipliedAlpha);
            GL.UseProgram(shader.ProgramId);

            GL.EnableVertexAttribArray(shader.AttributePositionLocation);
            fixed (float* ptr = vertex_buffer)
                GL.VertexAttribPointer(shader.AttributePositionLocation, 2, GL_FLOAT, 0, sizeof(float) * 2, (IntPtr)ptr);

            GL.EnableVertexAttribArray(shader.AttributeTexCoordLocation);
            fixed (float* ptr = uv_buffer)
                GL.VertexAttribPointer(shader.AttributeTexCoordLocation, 2, GL_FLOAT, 0, sizeof(float) * 2, (IntPtr)ptr);

            if (use_clipping_mask == true)
            {
                GL.ActiveTexture(GL_TEXTURE1);
                GL.BindTexture(GL_TEXTURE_2D, clipping_mask.TextureId);
                GL.Uniform1f(shader.SamplerTexture1Location, 1);

                GL.UniformMatrix4(shader.UniformClipMatrixLocation, false, ref clipping_matrix);

                GL.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            }
            else
            {
                GL.ActiveTexture(GL_TEXTURE1);
                GL.BindTexture(GL_TEXTURE_2D, 0);
            }

            GL.ActiveTexture(GL_TEXTURE0);
            GL.BindTexture(GL_TEXTURE_2D, texture.TextureId);
            GL.Uniform1f(shader.SamplerTexture0Location, 0);

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
                GL.DrawElements(GL_TRIANGLES, index_buffer.Length, GL_UNSIGNED_SHORT, (IntPtr)ptr);
        }

        public void EndDrawingModel()
        {
            State.RestoreState();
        }
    }
}