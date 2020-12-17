using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;


namespace Avalonia.Cubism.Render
{
    public class CubismAvaloniaState
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

        public CubismAvaloniaState(GlInterface gl)
        {
            this.GL = gl;
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
            
            GL.GetVertexAttrib(0, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[0]);
            GL.GetVertexAttrib(1, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[1]);
            GL.GetVertexAttrib(2, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[2]);
            GL.GetVertexAttrib(3, GL_VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[3]);

            LastScissorTest = GL.IsEnabled(GL_SCISSOR_TEST);
            LastStencilTest = GL.IsEnabled(GL_STENCIL_TEST);
            LastDepthTest = GL.IsEnabled(GL_DEPTH_TEST);
            LastCullFace = GL.IsEnabled(GL_CULL_FACE);
            LastBlend = GL.IsEnabled(GL_BLEND);

            GL.GetIntegerv(GL_FRONT_FACE, out LastFrontFace);

            GL.GetIntegerv(GL_COLOR_WRITEMASK, LastColorMask);
            
            GL.GetIntegerv(GL_BLEND_SRC_RGB, out LastBlending[0]);
            GL.GetIntegerv(GL_BLEND_DST_RGB, out LastBlending[1]);
            GL.GetIntegerv(GL_BLEND_SRC_ALPHA, out LastBlending[2]);
            GL.GetIntegerv(GL_BLEND_DST_ALPHA, out LastBlending[3]);

            GL.GetIntegerv(GL_FRAMEBUFFER_BINDING, out LastFrameBuffer);
            GL.GetIntegerv(GL_VIEWPORT, LastViewport);
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
            
            GL.FrontFace(LastFrontFace);

            GL.ColorMask(LastColorMask[0] != 0, LastColorMask[1] != 0, LastColorMask[2] != 0, LastColorMask[3] != 0);

            GL.BindBuffer(GL_ARRAY_BUFFER, LastArrayBufferBinding);
            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, LastElementArrayBufferBinding);

            GL.ActiveTexture(GL_TEXTURE1);
            GL.BindTexture(GL_TEXTURE_2D, LastTexture1Binding2D);

            GL.ActiveTexture(GL_TEXTURE0);
            GL.BindTexture(GL_TEXTURE_2D, LastTexture0Binding2D);

            GL.ActiveTexture(LastActiveTexture);
            
            GL.BlendFuncSeparate(LastBlending[0], LastBlending[1], LastBlending[2], LastBlending[3]);

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
                GL.Disable(cap);
        }

        private void SetEnabledVertexAttribArray(int index, bool enabled)
        {
            if (enabled)
                GL.EnableVertexAttribArray(index);
            else
                GL.DisableVertexAttribArray(index);
        }


    }
}