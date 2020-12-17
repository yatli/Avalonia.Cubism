using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

using CubismFramework;

using System;

namespace Avalonia.Cubism.Render
{
    public class CubismAvaloniaClippingMask : ICubismClippingMask, IDisposable
    {
        public int FrameBufferId { get; private set; }
        public int TextureId => Texture.TextureId;
        public int Width => Texture.Width;
        public int Height => Texture.Height;

        private readonly GlInterface GL;
        private CubismAvaloniaTexture Texture;
        private bool disposedValue = false;

        public CubismAvaloniaClippingMask(GlInterface gl, int width, int height)
        {
            this.GL = gl;
            Texture = new CubismAvaloniaTexture(gl, width, height);

            int[] fbos = new int[1];
            GL.GenFramebuffers(1, fbos);
            FrameBufferId = fbos[0];
            GL.BindFramebuffer(GL_FRAMEBUFFER, FrameBufferId);
            GL.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureId, 0);
            GL.BindFramebuffer(GL_FRAMEBUFFER, 0);
        }

        ~CubismAvaloniaClippingMask()
        {
            Dispose(false);
        }

        public void Resize(int width, int height)
        {
            GL.BindFramebuffer(GL_FRAMEBUFFER, FrameBufferId);
            GL.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
            Texture.Dispose();
            Texture = new CubismAvaloniaTexture(GL, width, height);
            GL.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureId, 0);
            GL.BindFramebuffer(GL_FRAMEBUFFER, 0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteFramebuffers(1, new int[1] { FrameBufferId });
                FrameBufferId = 0;
                Texture.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}