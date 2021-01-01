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
    class CubismAvaloniaRenderer : ICubismRenderer, IDisposable
    {
        private const int ClippingMaskSize = 256;
        private readonly GlInterfaceEx gl;
        private Matrix4x4 MvpMatrix;
        private float[] ModelColor = new float[4];
        private static float[] DefaultModelColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
        private CubismAvaloniaState State;
        private CubismAvaloniaShaderManager ShaderManager;
        private int m_maskVertexBuf;
        private int m_maskUvBuf;
        private int m_meshVertexBuf;
        private int m_meshUvBuf;
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
                    gl.BlendFuncSeparate(/*GL_ONE*/1, GL_ONE_MINUS_SRC_ALPHA, /*BlendingFactorSrc.One*/ 1, GL_ONE_MINUS_SRC_ALPHA);
                    break;

                case BlendModeType.Add:
                    gl.BlendFuncSeparate(/*BlendingFactorSrc.One*/ 1, /*BlendingFactorDest.One*/ 1, /*BlendingFactorSrc.Zero*/ 0, /*BlendingFactorDest.One*/ 1);
                    break;

                case BlendModeType.Multiply:
                    gl.BlendFuncSeparate(GL_DST_COLOR, GL_ONE_MINUS_SRC_ALPHA, /*BlendingFactorSrc.Zero*/ 0, /*BlendingFactorDest.One*/ 1);
                    break;
                }
            }
        }

        private bool UseCulling
        {
            set
            {
                if (value)
                    gl.Enable(GL_CULL_FACE);
                else
                    gl.Disable(GL_CULL_FACE);
            }
        }

        public bool UsePremultipliedAlpha { set; get; }

        public CubismAvaloniaRenderer(GlInterfaceEx gl)
        {
            this.gl = gl;
            State = new CubismAvaloniaState(gl);
            ShaderManager = new CubismAvaloniaShaderManager(gl);

            // create some buffer objects
            m_maskVertexBuf = gl.GenBuffer();
            m_maskUvBuf = gl.GenBuffer();
            m_meshVertexBuf = gl.GenBuffer();
            m_meshUvBuf = gl.GenBuffer();
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
                gl.DeleteBuffers(4, new[] { m_maskVertexBuf, m_maskUvBuf, m_meshVertexBuf, m_meshUvBuf });
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
            var clipping_mask = new CubismAvaloniaClippingMask(gl, ClippingMaskSize, ClippingMaskSize);
            ClippingMasks.Add(clipping_mask);
            return clipping_mask;
        }

        public ICubismTexture CreateTexture(byte[] texture_bytes)
        {
            var bitmap = new Bitmap(new MemoryStream(texture_bytes));
            var texture = new CubismAvaloniaTexture(gl, bitmap);
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


            gl.FrontFace(GL_CCW);
            
            gl.Disable(GL_SCISSOR_TEST);
            gl.Disable(GL_STENCIL_TEST);
            gl.Disable(GL_DEPTH_TEST);
            gl.Enable(GL_BLEND);
            gl.ColorMask(1, 1, 1, 1);

            gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
            gl.BindBuffer(GL_ARRAY_BUFFER, 0);

            if ((model_color != null) && (model_color.Length == 4))
                model_color.CopyTo(ModelColor, 0);
            else
                DefaultModelColor.CopyTo(ModelColor, 0);

            MvpMatrix = mvp_matrix;
        }

        public void StartDrawingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismAvaloniaClippingMask)iclipping_mask;
            gl.Viewport(0, 0, clipping_mask.Width, clipping_mask.Height);
            gl.BindFramebuffer(GL_FRAMEBUFFER, clipping_mask.FrameBufferId);

            gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            gl.Clear(GL_COLOR_BUFFER_BIT);

            var shader = ShaderManager.ShaderForDrawMask();
            gl.UseProgram(shader.ProgramId);

            gl.BlendFuncSeparate(/*BlendingFactorSrc.Zero*/ 0, GL_ONE_MINUS_SRC_COLOR, /*BlendingFactorSrc.Zero*/ 0, GL_ONE_MINUS_SRC_ALPHA);
        }

        unsafe public void DrawMask(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix4x4 clipping_matrix, bool use_culling, bool is_inverted_mask)
        {
            var texture = (CubismAvaloniaTexture)itexture;
            var clipping_mask = (CubismAvaloniaClippingMask)iclipping_mask;

            UseCulling = use_culling;

            var shader = ShaderManager.ShaderForDrawMask();

            gl.ActiveTexture(GL_TEXTURE0);
            gl.BindTexture(GL_TEXTURE_2D, texture.TextureId);
            gl.Uniform1i(shader.SamplerTexture0Location, 0);

            gl.EnableVertexAttribArray(shader.AttributePositionLocation);
            gl.BindBuffer(GL_ARRAY_BUFFER, m_maskVertexBuf);
            fixed (float* ptr = vertex_buffer)
                gl.BufferData(GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * vertex_buffer.Length), (IntPtr)ptr, GL_STATIC_DRAW);
            gl.VertexAttribPointer(shader.AttributePositionLocation, 2, GL_FLOAT, 0, sizeof(float)*2, IntPtr.Zero);

            gl.EnableVertexAttribArray(shader.AttributeTexCoordLocation);
            gl.BindBuffer(GL_ARRAY_BUFFER, m_maskUvBuf);
            fixed (float* ptr = uv_buffer)
                gl.BufferData(GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * uv_buffer.Length), (IntPtr)ptr, GL_STATIC_DRAW);
            gl.VertexAttribPointer(shader.AttributeTexCoordLocation, 2, GL_FLOAT, 0, 0, IntPtr.Zero);

            gl.Uniform4f(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            Matrix4x4* p = &clipping_matrix;
            gl.UniformMatrix4fv(shader.UniformClipMatrixLocation, 1, false, (float*)p);
            gl.Uniform4f(shader.UniformBaseColorLocation, -1.0f, -1.0f, 1.0f, 1.0f);

            fixed (short* ptr = index_buffer)
                gl.DrawElements(GL_TRIANGLES, index_buffer.Length, GL_UNSIGNED_SHORT, (IntPtr)ptr);
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
            gl.UseProgram(shader.ProgramId);

            gl.EnableVertexAttribArray(shader.AttributePositionLocation);
            gl.BindBuffer(GL_ARRAY_BUFFER, m_meshVertexBuf);
            fixed (float* ptr = vertex_buffer)
                gl.BufferData(GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * vertex_buffer.Length), (IntPtr)ptr, GL_STATIC_DRAW);
            gl.VertexAttribPointer(shader.AttributePositionLocation, 2, GL_FLOAT, 0, sizeof(float)*2, IntPtr.Zero);

            gl.EnableVertexAttribArray(shader.AttributeTexCoordLocation);
            gl.BindBuffer(GL_ARRAY_BUFFER, m_meshUvBuf);
            fixed (float* ptr = uv_buffer)
                gl.BufferData(GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * uv_buffer.Length), (IntPtr)ptr, GL_STATIC_DRAW);
            gl.VertexAttribPointer(shader.AttributeTexCoordLocation, 2, GL_FLOAT, 0, 0, IntPtr.Zero);


            if (use_clipping_mask == true)
            {
                gl.ActiveTexture(GL_TEXTURE1);
                gl.BindTexture(GL_TEXTURE_2D, clipping_mask.TextureId);
                gl.Uniform1i(shader.SamplerTexture1Location, 1);

                Matrix4x4* p = &clipping_matrix;
                gl.UniformMatrix4fv(shader.UniformClipMatrixLocation, 1, false, (float*)p);

                gl.Uniform4f(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            }
            else
            {
                gl.ActiveTexture(GL_TEXTURE1);
                gl.BindTexture(GL_TEXTURE_2D, 0);
            }

            gl.ActiveTexture(GL_TEXTURE0);
            gl.BindTexture(GL_TEXTURE_2D, texture.TextureId);
            gl.Uniform1i(shader.SamplerTexture0Location, 0);

            fixed(Matrix4x4* pmvp = &MvpMatrix) 
            {
                gl.UniformMatrix4fv(shader.UniformMatrixLocation, 1, false, (float*)pmvp);
            }

            float[] color = new float[4];
            ModelColor.CopyTo(color, 0);
            color[3] *= (float)opacity;
            if (UsePremultipliedAlpha == true)
            {
                color[0] *= color[3];
                color[1] *= color[3];
                color[2] *= color[3];
            }
            gl.Uniform4f(shader.UniformBaseColorLocation, color[0], color[1], color[2], color[3]);

            fixed (short* ptr = index_buffer)
                gl.DrawElements(GL_TRIANGLES, index_buffer.Length, GL_UNSIGNED_SHORT, (IntPtr)ptr);
        }

        public void EndDrawingModel()
        {
            State.RestoreState();
        }
    }
}