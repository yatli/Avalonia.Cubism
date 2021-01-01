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
        private readonly GlInterfaceEx gl;

        public CubismAvaloniaTexture(GlInterfaceEx gl, int width, int height)
        {
            this.gl = gl;
            var InternalFormat = gl.ContextInfo.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;
            Width = width;
            Height = height;
            var arr = new int[2];
            gl.GenTextures(1, arr);
            TextureId = arr[0];
            gl.BindTexture(GL_TEXTURE_2D, TextureId);
            gl.TexImage2D(GL_TEXTURE_2D, 0, InternalFormat, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
            SetupParameters(GL_LINEAR, GL_CLAMP_TO_EDGE);
            gl.BindTexture(GL_TEXTURE_2D, 0);
        }

        public CubismAvaloniaTexture(GlInterfaceEx gl, Bitmap _bitmap)
        {
            this.gl = gl;
            var InternalFormat = gl.ContextInfo.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;

            var arr = new int[2];
            gl.GenTextures(1, arr);
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
                gl.BindTexture(GL_TEXTURE_2D, TextureId);
                gl.PixelStorei(GL_UNPACK_ALIGNMENT, 4);
                gl.PixelStorei(GL_UNPACK_ROW_LENGTH, 0);
                // TODO restore pixel store mode
                gl.TexImage2D(GL_TEXTURE_2D, 0, InternalFormat, w, h, 0, GL_BGRA, GL_UNSIGNED_BYTE, (IntPtr)p);
                SetupParameters(GL_LINEAR, GL_CLAMP_TO_EDGE);
                gl.BindTexture(GL_TEXTURE_2D, 0);
            }

        }

        ~CubismAvaloniaTexture()
        {
            Dispose(false);
        }

        private void SetupParameters(int min_mag_filter, int wrap_mode)
        {
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_mag_filter);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, min_mag_filter);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_mode);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_mode);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                gl.DeleteTextures(1, new int[] { TextureId });
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