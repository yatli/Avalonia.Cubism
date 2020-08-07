using System;
using osuTK.Graphics.ES20;

namespace CubismFramework
{
    public class CubismOpenTKClippingMask : ICubismClippingMask, IDisposable
    {
        public int FrameBufferId { get; private set; }
        public int TextureId => Texture.TextureId;
        public int Width => Texture.Width;
        public int Height => Texture.Height;
        private CubismOpenTKTexture Texture;
        private bool disposedValue = false;

        public CubismOpenTKClippingMask(int width, int height)
        {
            Texture = new CubismOpenTKTexture(width, height);

            int[] fbos = new int[1];
            GL.GenFramebuffers(1, fbos);
            FrameBufferId = fbos[0];
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, TextureId, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        ~CubismOpenTKClippingMask()
        {
            Dispose(false);
        }

        public void Resize(int width, int height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, 0, 0);
            Texture.Dispose();
            Texture = new CubismOpenTKTexture(width, height);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, TextureId, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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