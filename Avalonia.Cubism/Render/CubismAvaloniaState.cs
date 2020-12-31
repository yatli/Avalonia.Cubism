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
        private readonly GlInterface GL;
        private readonly GlInterfaceEx GLex;

        public CubismAvaloniaState(GlInterface gl, GlInterfaceEx glex)
        {
            this.GL = gl;
            this.GLex = glex;
        }

        public void SaveState()
        {
            GL.GetIntegerv(GL_ARRAY_BUFFER_BINDING, out LastArrayBufferBinding);
            GL.GetIntegerv(GL_ELEMENT_ARRAY_BUFFER_BINDING, out LastElementArrayBufferBinding);
            GL.GetIntegerv(GL_CURRENT_PROGRAM, out LastProgram);

            GL.GetIntegerv(GL_ACTIVE_TEXTURE, out LastActiveTexture);
            GL.ActiveTexture(GL_TEXTURE1);
            GL.GetIntegerv(GL_TEXTURE_BINDING_2D, out LastTexture1Binding2D);

            GL.ActiveTexture(GL_TEXTURE0);
            GL.GetIntegerv(GL_TEXTURE_BINDING_2D, out LastTexture0Binding2D);
            
            GLex.GetVertexAttribiv(0, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[0]);
            GLex.GetVertexAttribiv(1, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[1]);
            GLex.GetVertexAttribiv(2, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[2]);
            GLex.GetVertexAttribiv(3, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[3]);

            LastScissorTest = GLex.IsEnabled(GL_SCISSOR_TEST) != 0;
            LastStencilTest = GLex.IsEnabled(GL_STENCIL_TEST) != 0;
            LastDepthTest = GLex.IsEnabled(GL_DEPTH_TEST) != 0;
            LastCullFace = GLex.IsEnabled(GL_CULL_FACE) != 0;
            LastBlend = GLex.IsEnabled(GL_BLEND) != 0;

            GL.GetIntegerv(GL_FRONT_FACE, out LastFrontFace);

            GLex.GetIntegerv(GL_COLOR_WRITEMASK, LastColorMask);
            
            GL.GetIntegerv(GL_BLEND_SRC_RGB, out LastBlending[0]);
            GL.GetIntegerv(GL_BLEND_DST_RGB, out LastBlending[1]);
            GL.GetIntegerv(GL_BLEND_SRC_ALPHA, out LastBlending[2]);
            GL.GetIntegerv(GL_BLEND_DST_ALPHA, out LastBlending[3]);

            GL.GetIntegerv(GL_FRAMEBUFFER_BINDING, out LastFrameBuffer);
            GLex.GetIntegerv(GL_VIEWPORT, LastViewport);
        }

        public void RestoreState()
        {
            GL.UseProgram(LastProgram);

            SetEnabledVertexAttribArray(0, LastVertexAttribArrayEnabled[0] != 0);
            SetEnabledVertexAttribArray(1, LastVertexAttribArrayEnabled[1] != 0);
            SetEnabledVertexAttribArray(2, LastVertexAttribArrayEnabled[2] != 0);
            SetEnabledVertexAttribArray(3, LastVertexAttribArrayEnabled[3] != 0);

            SetEnabled(GL_SCISSOR_TEST, LastScissorTest);
            SetEnabled(GL_STENCIL_TEST, LastStencilTest);
            SetEnabled(GL_DEPTH_TEST, LastDepthTest);
            SetEnabled(GL_CULL_FACE, LastCullFace);
            SetEnabled(GL_BLEND, LastBlend);
            
            GLex.FrontFace(LastFrontFace);

            static byte b2b(int v) => (v != 0) ? 1 : 0;

            GLex.ColorMask(b2b(LastColorMask[0]), b2b(LastColorMask[1]), b2b(LastColorMask[2]), b2b(LastColorMask[3]));

            GL.BindBuffer(GL_ARRAY_BUFFER, LastArrayBufferBinding);
            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, LastElementArrayBufferBinding);

            GL.ActiveTexture(GL_TEXTURE1);
            GL.BindTexture(GL_TEXTURE_2D, LastTexture1Binding2D);

            GL.ActiveTexture(GL_TEXTURE0);
            GL.BindTexture(GL_TEXTURE_2D, LastTexture0Binding2D);

            GL.ActiveTexture(LastActiveTexture);
            
            GLex.BlendFuncSeparate(LastBlending[0], LastBlending[1], LastBlending[2], LastBlending[3]);

            RestoreViewport();
            RestoreFrameBuffer();
        }

        public void RestoreViewport()
        {
            GL.Viewport(LastViewport[0], LastViewport[1], LastViewport[2], LastViewport[3]);
        }

        public void RestoreFrameBuffer()
        {
            GL.BindFramebuffer(GL_FRAMEBUFFER, LastFrameBuffer);
        }

        private void SetEnabled(int cap, bool enabled)
        {
            if (enabled)
                GL.Enable(cap);
            else
                GLex.Disable(cap);
        }

        private void SetEnabledVertexAttribArray(int index, bool enabled)
        {
            if (enabled)
                GL.EnableVertexAttribArray(index);
            else
                GLex.DisableVertexAttribArray(index);
        }
    }
}
