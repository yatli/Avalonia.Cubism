using System;
using System.Drawing;

using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.Platform;
using static Avalonia.OpenGL.GlConsts;

using CubismFramework;
using SkiaSharp;
using Avalonia.Skia;
using System.IO;
using System.Reflection;

namespace Avalonia.Cubism.Render
{
    unsafe class CubismAvaloniaTexture : ICubismTexture, IDisposable
    {
        public int TextureId { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private bool disposedValue = false;
        private GlInterface GL;

        public CubismAvaloniaTexture(GlInterface GL, int width, int height)
        {
            this.GL = GL;
            var InternalFormat = GL.ContextInfo.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;
            Width = width;
            Height = height;
            var arr = new int[2];
            GL.GenTextures(1, arr);
            TextureId = arr[0];
            GL.BindTexture(GL_TEXTURE_2D, TextureId);
            GL.TexImage2D(GL_TEXTURE_2D, 0, InternalFormat, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
            SetupParameters(GL_LINEAR, GL_CLAMP_TO_EDGE);
            GL.BindTexture(GL_TEXTURE_2D, 0);
        }

        public CubismAvaloniaTexture(GlInterface GL, GlInterfaceEx GLex, Bitmap _bitmap)
        {
            this.GL = GL;
            var InternalFormat = GL.ContextInfo.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;

            var arr = new int[2];
            GL.GenTextures(1, arr);
            TextureId = arr[0];

            var impl = _bitmap.PlatformImpl.Item;
            var field = impl.GetType().GetField("_image", BindingFlags.NonPublic | BindingFlags.Instance);
            int w = _bitmap.PixelSize.Width;
            int h = _bitmap.PixelSize.Height;
            var img = (SKImage)field.GetValue(impl);
            var info = new SKImageInfo(w, h, SKColorType.Bgra8888);
            byte[] buf = new byte[w * h * 4];
            fixed (byte* p = buf)
            {
                img.ReadPixels(info, (IntPtr)p);
                GL.BindTexture(GL_TEXTURE_2D, TextureId);
                GLex.PixelStorei(GL_UNPACK_ALIGNMENT, 4);
                GLex.PixelStorei(GL_UNPACK_ROW_LENGTH, 0);
                // TODO restore pixel store mode
                GL.TexImage2D(GL_TEXTURE_2D, 0, InternalFormat, w, h, 0, GL_BGRA, GL_UNSIGNED_BYTE, (IntPtr)p);
                SetupParameters(GL_LINEAR, GL_CLAMP_TO_EDGE);
                GL.BindTexture(GL_TEXTURE_2D, 0);
            }

        }

        ~CubismAvaloniaTexture()
        {
            Dispose(false);
        }

        private void SetupParameters(int min_mag_filter, int wrap_mode)
        {
            GL.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_mag_filter);
            GL.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, min_mag_filter);
            GL.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_mode);
            GL.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_mode);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteTextures(1, new int[] { TextureId });
                TextureId = 0;
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
    }
}