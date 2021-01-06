using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;


namespace Avalonia.Cubism.Render
{
    class CubismAvaloniaState
    {
        private int LastArrayBufferBinding;
        private int LastElementArrayBufferBinding;
        private int LastProgram;
        private int LastActiveTexture;
        private int LastTexture0Binding2D;
        private int LastTexture1Binding2D;
        private int[] LastVertexAttribArrayEnabled = new int[4];
        private bool LastScissorTest;
        private bool LastBlend;
        private bool LastStencilTest;
        private bool LastDepthTest;
        private bool LastCullFace;
        private int LastFrontFace;
        private int[] LastColorMask = new int[4];
        private int[] LastBlending = new int[4];
        private int LastFrameBuffer;
        private int[] LastViewport = new int[4];
        private readonly GlInterfaceEx gl;

        public CubismAvaloniaState(GlInterfaceEx gl)
        {
            this.gl = gl;
        }

        public void SaveState()
        {
            gl.GetIntegerv(GL_ARRAY_BUFFER_BINDING, out LastArrayBufferBinding);
            gl.GetIntegerv(GL_ELEMENT_ARRAY_BUFFER_BINDING, out LastElementArrayBufferBinding);
            gl.GetIntegerv(GL_CURRENT_PROGRAM, out LastProgram);

            gl.GetIntegerv(GL_ACTIVE_TEXTURE, out LastActiveTexture);
            gl.ActiveTexture(GL_TEXTURE1);
            gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out LastTexture1Binding2D);

            gl.ActiveTexture(GL_TEXTURE0);
            gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out LastTexture0Binding2D);
            
            gl.GetVertexAttribiv(0, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[0]);
            gl.GetVertexAttribiv(1, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[1]);
            gl.GetVertexAttribiv(2, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[2]);
            gl.GetVertexAttribiv(3, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[3]);

            LastScissorTest = gl.IsEnabled(GL_SCISSOR_TEST) != 0;
            LastStencilTest = gl.IsEnabled(GL_STENCIL_TEST) != 0;
            LastDepthTest = gl.IsEnabled(GL_DEPTH_TEST) != 0;
            LastCullFace = gl.IsEnabled(GL_CULL_FACE) != 0;
            LastBlend = gl.IsEnabled(GL_BLEND) != 0;

            gl.GetIntegerv(GL_FRONT_FACE, out LastFrontFace);

            gl.GetIntegerva(GL_COLOR_WRITEMASK, LastColorMask);
            
            gl.GetIntegerv(GL_BLEND_SRC_RGB, out LastBlending[0]);
            gl.GetIntegerv(GL_BLEND_DST_RGB, out LastBlending[1]);
            gl.GetIntegerv(GL_BLEND_SRC_ALPHA, out LastBlending[2]);
            gl.GetIntegerv(GL_BLEND_DST_ALPHA, out LastBlending[3]);

            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out LastFrameBuffer);
            gl.GetIntegerva(GL_VIEWPORT, LastViewport);
        }

        public void RestoreState()
        {
            gl.UseProgram(LastProgram);

            SetEnabledVertexAttribArray(0, LastVertexAttribArrayEnabled[0] != 0);
            SetEnabledVertexAttribArray(1, LastVertexAttribArrayEnabled[1] != 0);
            SetEnabledVertexAttribArray(2, LastVertexAttribArrayEnabled[2] != 0);
            SetEnabledVertexAttribArray(3, LastVertexAttribArrayEnabled[3] != 0);

            SetEnabled(GL_SCISSOR_TEST, LastScissorTest);
            SetEnabled(GL_STENCIL_TEST, LastStencilTest);
            SetEnabled(GL_DEPTH_TEST, LastDepthTest);
            SetEnabled(GL_CULL_FACE, LastCullFace);
            SetEnabled(GL_BLEND, LastBlend);
            
            gl.FrontFace(LastFrontFace);

            static byte b2b(int v) => (byte)((v != 0) ? 1 : 0);

            gl.ColorMask(b2b(LastColorMask[0]), b2b(LastColorMask[1]), b2b(LastColorMask[2]), b2b(LastColorMask[3]));

            gl.BindBuffer(GL_ARRAY_BUFFER, LastArrayBufferBinding);
            gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, LastElementArrayBufferBinding);

            gl.ActiveTexture(GL_TEXTURE1);
            gl.BindTexture(GL_TEXTURE_2D, LastTexture1Binding2D);

            gl.ActiveTexture(GL_TEXTURE0);
            gl.BindTexture(GL_TEXTURE_2D, LastTexture0Binding2D);

            gl.ActiveTexture(LastActiveTexture);
            
            gl.BlendFuncSeparate(LastBlending[0], LastBlending[1], LastBlending[2], LastBlending[3]);

            RestoreViewport();
            RestoreFrameBuffer();
        }

        public void RestoreViewport()
        {
            gl.Viewport(LastViewport[0], LastViewport[1], LastViewport[2], LastViewport[3]);
        }

        public void RestoreFrameBuffer()
        {
            gl.BindFramebuffer(GL_FRAMEBUFFER, LastFrameBuffer);
        }

        private void SetEnabled(int cap, bool enabled)
        {
            if (enabled)
                gl.Enable(cap);
            else
                gl.Disable(cap);
        }

        private void SetEnabledVertexAttribArray(int index, bool enabled)
        {
            if (enabled)
                gl.EnableVertexAttribArray(index);
            else
                gl.DisableVertexAttribArray(index);
        }
    }
}
