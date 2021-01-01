using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

using CubismFramework;

using System;

namespace Avalonia.Cubism.Render
{
    class CubismAvaloniaClippingMask : ICubismClippingMask, IDisposable
    {
        public int FrameBufferId { get; private set; }
        public int TextureId => Texture.TextureId;
        public int Width => Texture.Width;
        public int Height => Texture.Height;

        private readonly GlInterfaceEx gl;
        private CubismAvaloniaTexture Texture;
        private bool disposedValue = false;

        public CubismAvaloniaClippingMask(GlInterfaceEx gl, int width, int height)
        {
            this.gl = gl;
            Texture = new CubismAvaloniaTexture(gl, width, height);

            int[] fbos = new int[1];
            this.gl.GenFramebuffers(1, fbos);
            FrameBufferId = fbos[0];
            this.gl.BindFramebuffer(GL_FRAMEBUFFER, FrameBufferId);
            this.gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureId, 0);
            this.gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
        }

        ~CubismAvaloniaClippingMask()
        {
            Dispose(false);
        }

        public void Resize(int width, int height)
        {
            gl.BindFramebuffer(GL_FRAMEBUFFER, FrameBufferId);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
            Texture.Dispose();
            Texture = new CubismAvaloniaTexture(gl, width, height);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureId, 0);
            gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                gl.DeleteFramebuffers(1, new int[1] { FrameBufferId });
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