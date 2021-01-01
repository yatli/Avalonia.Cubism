using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

using System;
using Avalonia.Platform.Interop;
using System.Text;
using System.Runtime.InteropServices;

namespace Avalonia.Cubism.Render
{
    class CubismAvaloniaShaderManager : IDisposable
    {
        private const int MaxErrorLength = 1024;
        private readonly GLShaderProgram MaskDrawingShader;
        private readonly GLShaderProgram UnmaskedMeshDrawingShader;
        private readonly GLShaderProgram MaskedMeshDrawingShader;
        private readonly GLShaderProgram UnmaskedPremultipliedAlphaMeshDrawingShader;
        private readonly GLShaderProgram MaskedPremultipliedAlphaMeshDrawingShader;
        private bool disposedValue = false;

        public CubismAvaloniaShaderManager(GlInterface gl)
        {
            MaskDrawingShader = new SetupMaskShaderProgram(gl);
            UnmaskedMeshDrawingShader = new UnmaskedShaderProgram(gl);
            MaskedMeshDrawingShader = new MaskedShaderProgram(gl);
            UnmaskedPremultipliedAlphaMeshDrawingShader = new UnmaskedPremultipliedAlphaShaderProgram(gl);
            MaskedPremultipliedAlphaMeshDrawingShader = new MaskedPremultipliedAlphaShaderProgram(gl);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                MaskDrawingShader.Dispose();
                UnmaskedMeshDrawingShader.Dispose();
                MaskedMeshDrawingShader.Dispose();
                UnmaskedPremultipliedAlphaMeshDrawingShader.Dispose();
                MaskedPremultipliedAlphaMeshDrawingShader.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public GLShaderProgram ShaderForDrawMask()
        {
            return MaskDrawingShader;
        }

        public GLShaderProgram ShaderForDrawMesh(bool use_clipping_mask, bool use_premultipled_alpha)
        {
            if (use_clipping_mask == false)
            {
                if (use_premultipled_alpha == false)
                {
                    return UnmaskedMeshDrawingShader;
                }
                else
                {
                    return UnmaskedPremultipliedAlphaMeshDrawingShader;
                }
            }
            else
            {
                if (use_premultipled_alpha == false)
                {
                    return MaskedMeshDrawingShader;
                }
                else
                {
                    return MaskedPremultipliedAlphaMeshDrawingShader;
                }
            }
        }

        public unsafe class GLShader : IDisposable
        {
            private readonly GlInterface gl;
            public readonly int ShaderId;
            private bool disposedValue = false;

            public GLShader(GlInterface gl, int shaderType, string source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                this.gl = gl;

                ShaderId = gl.CreateShader(shaderType);
                using (var b = new Utf8Buffer(source))
                {
                    void*[] pstr = new void*[2];
                    long[] len = new long[2];
                    pstr[0] = (void*)b.DangerousGetHandle();
                    len[0] = b.ByteLen;
                    fixed (void** ppstr = pstr)
                    fixed (long* plen = len)
                        gl.ShaderSource(ShaderId, 1, (IntPtr)ppstr, (IntPtr)plen);
                }
                gl.CompileShader(ShaderId);
                int compile_succeeded;
                gl.GetShaderiv(ShaderId, GL_COMPILE_STATUS, &compile_succeeded);

                if (compile_succeeded == 0)
                {
                    byte[] buf = new byte[MaxErrorLength];
                    string log;
                    fixed (byte* pbuf = buf)
                    {
                        gl.GetShaderInfoLog(ShaderId, MaxErrorLength, out int log_length, pbuf);
                        log = Encoding.UTF8.GetString(pbuf, log_length);
                    }
                    throw new InvalidOperationException($"Failed to compile shader : {log}");
                }
            }

            ~GLShader()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    gl.DeleteShader(ShaderId);
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        public unsafe class GLShaderProgram : IDisposable
        {
            private readonly GlInterface gl;
            private readonly GlVersion GlVersion;

            public int ProgramId { get; private set; }
            public int AttributePositionLocation { get; protected set; } = -1;
            public int AttributeTexCoordLocation { get; protected set; } = -1;
            public int SamplerTexture0Location { get; protected set; } = -1;
            public int SamplerTexture1Location { get; protected set; } = -1;
            public int UniformMatrixLocation { get; protected set; } = -1;
            public int UniformClipMatrixLocation { get; protected set; } = -1;
            public int UnifromChannelFlagLocation { get; protected set; } = -1;
            public int UniformBaseColorLocation { get; protected set; } = -1;
            private bool disposedValue = false;

            private readonly GLShader vertex_shader;
            private readonly GLShader fragment_shader;

            public GLShaderProgram(GlInterface gl, string vertex_shader_source, string fragment_shader_source)
            {
                this.gl = gl;
                this.GlVersion = gl.ContextInfo.Version;

                vertex_shader_source = GetShader(false, vertex_shader_source);
                fragment_shader_source = GetShader(true, fragment_shader_source);

                vertex_shader = new GLShader(gl, GL_VERTEX_SHADER, vertex_shader_source);
                fragment_shader = new GLShader(gl, GL_FRAGMENT_SHADER, fragment_shader_source);

                ProgramId = gl.CreateProgram();
                gl.AttachShader(ProgramId, vertex_shader.ShaderId);
                gl.AttachShader(ProgramId, fragment_shader.ShaderId);
                gl.LinkProgram(ProgramId);
                int link_succeeded;
                gl.GetProgramiv(ProgramId, GL_LINK_STATUS, &link_succeeded);

                if (link_succeeded == 0)
                {
                    byte[] buf = new byte[MaxErrorLength];
                    string log;
                    fixed (byte* pbuf = buf)
                    {
                        gl.GetProgramInfoLog(ProgramId, MaxErrorLength, out int log_length, pbuf);
                        log = Encoding.UTF8.GetString(pbuf, log_length);
                    }
                    throw new InvalidOperationException($"Failed to link program : {log}");
                }
            }

            ~GLShaderProgram()
            {
                Dispose(false);
            }


            public int AttributeLocation(string attribute_name)
            {
                int location = gl.GetAttribLocationString(ProgramId, attribute_name);
                if (location < 0)
                    throw new InvalidOperationException($"No attribute {attribute_name}");

                return location;
            }

            public int UniformLocation(string uniform_name)
            {
                int location = gl.GetUniformLocationString(ProgramId, uniform_name);
                if (location < 0)
                    throw new InvalidOperationException($"No uniform {uniform_name}");

                return location;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    gl.DeleteProgram(ProgramId);
                    ProgramId = 0;
                    vertex_shader.Dispose();
                    fragment_shader.Dispose();
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            private string GetShader(bool fragment, string shader)
            {
                var version = (GlVersion.Type == GlProfileType.OpenGL ?
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120 :
                    100);
                var data = "#version " + version + "\n";
                if (GlVersion.Type == GlProfileType.OpenGLES)
                    data += "precision mediump float;\n";
                if (version >= 150)
                {
                    shader = shader.Replace("attribute", "in");
                    if (fragment)
                        shader = shader
                            .Replace("varying", "in")
                            .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
                            .Replace("gl_FragColor", "outFragColor");
                    else
                        shader = shader.Replace("varying", "out");
                }

                data += shader;

                return data;
            }
        }

        public class SetupMaskShaderProgram : GLShaderProgram
        {
            private static readonly string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            varying vec4 v_myPos;
            uniform mat4 u_clipMatrix;
            void main() {
                gl_Position = u_clipMatrix * a_position;
                v_myPos = u_clipMatrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }";

            private static readonly string FragmentShaderSource = @"
            varying vec2 v_texCoord;
            varying vec4 v_myPos;
            uniform sampler2D s_texture0;
            uniform vec4 u_channelFlag;
            uniform vec4 u_baseColor;
            void main() {
                float isInside =
                    step(u_baseColor.x, v_myPos.x / v_myPos.w) *
                    step(u_baseColor.y, v_myPos.y / v_myPos.w) *
                    step(v_myPos.x / v_myPos.w, u_baseColor.z) *
                    step(v_myPos.y / v_myPos.w, u_baseColor.w);
                gl_FragColor = u_channelFlag * texture2D(s_texture0 , v_texCoord).a * isInside;
            }";

            public SetupMaskShaderProgram(GlInterface gl)
                : base(gl, VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
        }

        public class UnmaskedShaderProgram : GLShaderProgram
        {
            private static readonly string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            uniform mat4 u_matrix;
            void main() {
                gl_Position = u_matrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }";

            private static readonly string FragmentShaderSource = @"
            varying vec2 v_texCoord;
            uniform sampler2D s_texture0;
            uniform vec4 u_baseColor;
            void main() {
                vec4 color = texture2D(s_texture0 , v_texCoord) * u_baseColor;
                gl_FragColor = vec4(color.rgb * color.a,  color.a);
            }";

            public UnmaskedShaderProgram(GlInterface gl)
                : base(gl, VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
        }

        public class MaskedShaderProgram : GLShaderProgram
        {
            private static readonly string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            varying vec4 v_clipPos;
            uniform mat4 u_matrix;
            uniform mat4 u_clipMatrix;
            void main() {
                gl_Position = u_matrix * a_position;
                v_clipPos = u_clipMatrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }";

            private static readonly string FragmentShaderSource = @"
            varying vec2 v_texCoord;
            varying vec4 v_clipPos;
            uniform sampler2D s_texture0;
            uniform sampler2D s_texture1;
            uniform vec4 u_channelFlag;
            uniform vec4 u_baseColor;
            void main() {
                vec4 col_formask = texture2D(s_texture0 , v_texCoord) * u_baseColor;
                col_formask.rgb = col_formask.rgb  * col_formask.a ;
                vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
                float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
                col_formask = col_formask * maskVal;
                gl_FragColor = col_formask;
            }";

            public MaskedShaderProgram(GlInterface gl)
                : base(gl, VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                SamplerTexture1Location = UniformLocation("s_texture1");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
        }

        public class UnmaskedPremultipliedAlphaShaderProgram : GLShaderProgram
        {
            private static readonly string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            uniform mat4 u_matrix;
            void main() {
                gl_Position = u_matrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }
            ";

            private static readonly string FragmentShaderSource = @"
            varying vec2 v_texCoord;
            uniform sampler2D s_texture0;
            uniform vec4 u_baseColor;
            void main() {
                gl_FragColor = texture2D(s_texture0 , v_texCoord) * u_baseColor;
            }
            ";

            public UnmaskedPremultipliedAlphaShaderProgram(GlInterface gl)
                : base(gl, VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
        }

        public class MaskedPremultipliedAlphaShaderProgram : GLShaderProgram
        {
            private static readonly string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            varying vec4 v_clipPos;
            uniform mat4 u_matrix;
            uniform mat4 u_clipMatrix;
            void main() {
                gl_Position = u_matrix * a_position;
                v_clipPos = u_clipMatrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }
            ";

            private static readonly string FragmentShaderSource = @"
            varying vec2 v_texCoord;
            varying vec4 v_clipPos;
            uniform sampler2D s_texture0;
            uniform sampler2D s_texture1;
            uniform vec4 u_channelFlag;
            uniform vec4 u_baseColor;
            void main() {
                vec4 col_formask = texture2D(s_texture0 , v_texCoord) * u_baseColor;
                vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
                float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
                col_formask = col_formask * maskVal;
                gl_FragColor = col_formask;
            }";

            public MaskedPremultipliedAlphaShaderProgram(GlInterface gl)
                : base(gl, VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                SamplerTexture1Location = UniformLocation("s_texture1");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
        }
    }
}