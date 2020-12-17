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

namespace Avalonia.Cubism.Render
{
    public unsafe class CubismAvaloniaTexture : ICubismTexture, IDisposable
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
            SetupParameters(GL_TEXTURE_MIN_FILTER, GL_CLAMP_TO_EDGE);
            GL.BindTexture(GL_TEXTURE_2D, 0);
        }

        public CubismAvaloniaTexture(GlInterface GL, Bitmap bitmap)
        {
            this.GL = GL;
            var InternalFormat = GL.ContextInfo.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;
            int source_format;
            switch(SKImageInfo.PlatformColorType.ToPixelFormat())
            {
                case PixelFormat.Bgra8888:
                    source_format = GL_BGRA;
                    break;
                case PixelFormat.Rgba8888:
                    source_format = GL_RGBA;
                    break;
                default:
                    throw new Exception();
            }

            var arr = new int[2];
            GL.GenTextures(1, arr);
            TextureId = arr[0];

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms);
                data = ms.ToArray();
            }

            GL.BindTexture(GL_TEXTURE_2D, TextureId);
            fixed(byte* p = data)
            {
                GL.TexImage2D(GL_TEXTURE_2D, 0, InternalFormat, bitmap.PixelSize.Width, bitmap.PixelSize.Height, 0, source_format, GL_UNSIGNED_BYTE, new IntPtr(p));
            }
            GL.TexParameteri(GL_TEXTURE_2D, GL_UNPACK_ALIGNMENT, 4);
            GL.TexParameteri(GL_TEXTURE_2D, GL_UNPACK_ROW_LENGTH, 0);
            SetupParameters(GL_TEXTURE_MIN_FILTER, GL_CLAMP_TO_EDGE);
            GL.BindTexture(GL_TEXTURE_2D, 0);
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