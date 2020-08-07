using System;
using osuTK.Graphics.ES30;

namespace CubismFramework
{
    public class CubismOpenTKShaderManager : IDisposable
    {
        private const int MaxErrorLength = 1024;
        private GLShaderProgram MaskDrawingShader;
        private GLShaderProgram UnmaskedMeshDrawingShader;
        private GLShaderProgram MaskedMeshDrawingShader;
        private GLShaderProgram UnmaskedPremultipliedAlphaMeshDrawingShader;
        private GLShaderProgram MaskedPremultipliedAlphaMeshDrawingShader;
        private bool disposedValue = false;

        public CubismOpenTKShaderManager()
        {
            MaskDrawingShader = new SetupMaskShaderProgram();
            UnmaskedMeshDrawingShader = new UnmaskedShaderProgram();
            MaskedMeshDrawingShader = new MaskedShaderProgram();
            UnmaskedPremultipliedAlphaMeshDrawingShader = new UnmaskedPremultipliedAlphaShaderProgram();
            MaskedPremultipliedAlphaMeshDrawingShader = new MaskedPremultipliedAlphaShaderProgram();
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

        public class GLShader : IDisposable
        {
            public readonly int ShaderId;
            private bool disposedValue = false;

            public GLShader(ShaderType shaderType, string source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                ShaderId = GL.CreateShader(shaderType);
                GL.ShaderSource(ShaderId, source);
                GL.CompileShader(ShaderId);
                GL.GetShader(ShaderId, ShaderParameter.CompileStatus, out int compile_succeeded);
                
                if (compile_succeeded == 0)
                {
                    GL.GetShaderInfoLog(ShaderId, MaxErrorLength, out int log_length, out string log);
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
                    GL.DeleteShader(ShaderId);
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        public class GLShaderProgram : IDisposable
        {
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

            public GLShaderProgram(string vertex_shader_source, string fragment_shader_source)
            {
                using (GLShader vertex_shader = new GLShader(ShaderType.VertexShader, vertex_shader_source))
                using (GLShader fragment_shader = new GLShader(ShaderType.FragmentShader, fragment_shader_source))
                {
                    ProgramId = GL.CreateProgram();
                    GL.AttachShader(ProgramId, vertex_shader.ShaderId);
                    GL.AttachShader(ProgramId, fragment_shader.ShaderId);
                    GL.LinkProgram(ProgramId);
                    GL.GetProgram(ProgramId, GetProgramParameterName.LinkStatus, out int link_succeeded);

                    if (link_succeeded == 0)
                    {
                        GL.GetProgramInfoLog(ProgramId, MaxErrorLength, out int log_length, out string log);
                        throw new InvalidOperationException($"Failed to link program : {log}");
                    }
                }
            }

            ~GLShaderProgram()
            {
                Dispose(false);
            }


            public int AttributeLocation(string attribute_name)
            {
                int location = GL.GetAttribLocation(ProgramId, attribute_name);
                if (location < 0)
                    throw new InvalidOperationException($"No attribute {attribute_name}");

                return location;
            }

            public int UniformLocation(string uniform_name)
            {
                int location = GL.GetUniformLocation(ProgramId, uniform_name);
                if (location < 0)
                    throw new InvalidOperationException($"No uniform {uniform_name}");

                return location;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    GL.DeleteProgram(ProgramId);
                    ProgramId = 0;
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        public class SetupMaskShaderProgram : GLShaderProgram
        {
            private static string VertexShaderSource = @"
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

            private static string FragmentShaderSource = @"
            precision mediump float;
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

            public SetupMaskShaderProgram()
                : base(VertexShaderSource, FragmentShaderSource)
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
            private static string VertexShaderSource = @"
            attribute vec4 a_position;
            attribute vec2 a_texCoord;
            varying vec2 v_texCoord;
            uniform mat4 u_matrix;
            void main() {
                gl_Position = u_matrix * a_position;
                v_texCoord = a_texCoord;
                v_texCoord.y = 1.0 - v_texCoord.y;
            }";

            private static string FragmentShaderSource = @"
            precision mediump float;
            varying vec2 v_texCoord;
            uniform sampler2D s_texture0;
            uniform vec4 u_baseColor;
            void main() {
                vec4 color = texture2D(s_texture0 , v_texCoord) * u_baseColor;
                gl_FragColor = vec4(color.rgb * color.a,  color.a);
            }";

            public UnmaskedShaderProgram()
                : base(VertexShaderSource, FragmentShaderSource)
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
            private static string VertexShaderSource = @"
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

            private static string FragmentShaderSource = @"
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

            public MaskedShaderProgram()
                : base(VertexShaderSource, FragmentShaderSource)
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
            private static string VertexShaderSource = @"
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

            private static string FragmentShaderSource = @"
            precision mediump float;
            varying vec2 v_texCoord;
            uniform sampler2D s_texture0;
            uniform vec4 u_baseColor;
            void main() {
                gl_FragColor = texture2D(s_texture0 , v_texCoord) * u_baseColor;
            }
            ";

            public UnmaskedPremultipliedAlphaShaderProgram()
                : base(VertexShaderSource, FragmentShaderSource)
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
            private static string VertexShaderSource = @"
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

            private static string FragmentShaderSource = @"
            precision mediump float;
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

            public MaskedPremultipliedAlphaShaderProgram()
                : base(VertexShaderSource, FragmentShaderSource)
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