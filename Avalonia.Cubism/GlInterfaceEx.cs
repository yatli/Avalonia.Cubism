using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace Avalonia.Cubism
{
    public delegate void GlGetIntegerva(int name, int[] rv);
    public delegate void GlBlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha);
    public delegate void GlDisable(int cap);
    public delegate void GlFrontFace(int cap);
    public delegate void GlColorMask(byte r, byte g, byte b, byte a);
    public delegate void GlUniform4f(int location, float x, float y, float z, float w);
    public delegate byte GlIsEnabled(int cap);
    public delegate void GlGetVertexAttribiv(int idx, int name, out int value);
    public delegate void GlDisableVertexAttribArray(int idx);
    public delegate void GlPixelStorei(int param, int val);
    public delegate void GlUniform1i(int param, int value);
    public unsafe delegate void GlDebugProc(int src, int ty, int id, int sev, int len, byte* msg, void* userparam);
    public unsafe delegate void GlDebugMessageCallback(GlDebugProc callback, void* userparam);
    public delegate void GlGenVertexArrays(int n, int[] rv);
    public delegate void GlBindVertexArray(int array);
    public delegate void GlDeleteVertexArrays(int size, int[] rv);

    unsafe class GlInterfaceEx : GlInterface
    {
        public GlInterfaceEx(GlInterface GL): base(GL.ContextInfo.Version, GL.GetProcAddress)
        {
            GetIntegerva = GL.GetProcAddress<GlGetIntegerva>("glGetIntegerv");
            BlendFuncSeparate = GL.GetProcAddress<GlBlendFuncSeparate>("glBlendFuncSeparate");
            Disable = GL.GetProcAddress<GlDisable>("glDisable");
            FrontFace = GL.GetProcAddress<GlFrontFace>("glFrontFace");
            ColorMask = GL.GetProcAddress<GlColorMask>("glColorMask");
            Uniform4f = GL.GetProcAddress<GlUniform4f>("glUniform4f");
            IsEnabled = GL.GetProcAddress<GlIsEnabled>("glIsEnabled");
            GetVertexAttribiv = GL.GetProcAddress<GlGetVertexAttribiv>("glGetVertexAttribiv");
            DisableVertexAttribArray = GL.GetProcAddress<GlDisableVertexAttribArray>("glDisableVertexAttribArray");
            PixelStorei = GL.GetProcAddress<GlPixelStorei>("glPixelStorei");
            Uniform1i = GL.GetProcAddress<GlUniform1i>("glUniform1i");
            DebugMessageCallback = GL.GetProcAddress<GlDebugMessageCallback>("glDebugMessageCallback");
            GenVertexArrays = GL.GetProcAddress<GlGenVertexArrays>("glGenVertexArrays");
            BindVertexArray = GL.GetProcAddress<GlBindVertexArray>("glBindVertexArray");
            DeleteVertexArrays = GL.GetProcAddress<GlDeleteVertexArrays>("glDeleteVertexArrays");
        }

        // https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/samples/ControlCatalog/Pages/OpenGlPage.xaml.cs#L381
        public GlGetIntegerva GetIntegerva { get; }
        public GlBlendFuncSeparate BlendFuncSeparate { get; }
        public GlDisable Disable { get; }
        public GlFrontFace FrontFace { get; }
        public GlColorMask ColorMask { get; }
        public GlUniform4f Uniform4f { get; }
        public GlIsEnabled IsEnabled { get; }
        public GlGetVertexAttribiv GetVertexAttribiv { get; }
        public GlDisableVertexAttribArray DisableVertexAttribArray {get; }
        public GlPixelStorei PixelStorei { get; }
        public GlUniform1i Uniform1i { get; }
        public GlDebugMessageCallback DebugMessageCallback { get; }
        public GlGenVertexArrays GenVertexArrays { get; }
        public GlBindVertexArray BindVertexArray { get; }
        public GlDeleteVertexArrays DeleteVertexArrays { get; }

    }
    public static class GlInterfaceExtension
    {
        public static void CheckError(this GlInterface gl, [CallerMemberName] string caller = "")
        {
            GlErrors err;
            while ((err = (GlErrors)gl.GetError()) != GL_NO_ERROR)
            {
                Console.WriteLine($"{caller}: {err}");
            }
        }
    }
}
