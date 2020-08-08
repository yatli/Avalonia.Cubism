using System;
using System.Drawing;
using osuTK.Graphics.OpenGL;
using Imaging = System.Drawing.Imaging;

namespace CubismFramework
{
    public class CubismOpenTKTexture : ICubismTexture, IDisposable
    {
        public int TextureId { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private bool disposedValue = false;

         public CubismOpenTKTexture(int width, int height)
        {
            Width = width;
            Height = height;
            TextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            SetupParameters((int)TextureMinFilter.Linear, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public CubismOpenTKTexture(Bitmap source_bitmap)
        {
            PixelFormat source_format;
            int alignment;
            switch (source_bitmap.PixelFormat)
            {
                case Imaging.PixelFormat.Format24bppRgb:
                    source_format = PixelFormat.Bgr;
                    alignment = 1;
                    break;
                case Imaging.PixelFormat.Format32bppRgb:
                    source_format = PixelFormat.Bgr;
                    alignment = 4;
                    break;
                case Imaging.PixelFormat.Format32bppArgb:
                    source_format = PixelFormat.Bgra;
                    alignment = 4;
                    break;
                case Imaging.PixelFormat.Format32bppPArgb:
                    source_format = PixelFormat.Bgra;
                    alignment = 4;
                    break;
                default:
                    throw new ArgumentException();
            }
            
            TextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureId);
            Imaging.BitmapData data = source_bitmap.LockBits(new Rectangle(0, 0, source_bitmap.Width, source_bitmap.Height), Imaging.ImageLockMode.ReadOnly, source_bitmap.PixelFormat);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, alignment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, source_format, PixelType.UnsignedByte, data.Scan0);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
            source_bitmap.UnlockBits(data);
            SetupParameters((int)TextureMinFilter.Linear, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        ~CubismOpenTKTexture()
        {
            Dispose(false);
        }

        private void SetupParameters(int min_mag_filter, int wrap_mode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, min_mag_filter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, min_mag_filter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrap_mode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrap_mode);
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