using osuTK.Graphics.ES20;

namespace CubismFramework
{
    public class CubismOpenTKState
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

        public void SaveState()
        {
            GL.GetInteger(GetPName.ArrayBufferBinding, out LastArrayBufferBinding);
            GL.GetInteger(GetPName.ElementArrayBufferBinding, out LastElementArrayBufferBinding);
            GL.GetInteger(GetPName.CurrentProgram, out LastProgram);

            GL.GetInteger(GetPName.ActiveTexture, out LastActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.GetInteger(GetPName.TextureBinding2D, out LastTexture1Binding2D);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GetInteger(GetPName.TextureBinding2D, out LastTexture0Binding2D);
            
            GL.GetVertexAttrib(0, VertexAttribParameter.VertexAttribArrayEnabled, out LastVertexAttribArrayEnabled[0]);
            GL.GetVertexAttrib(1, VertexAttribParameter.VertexAttribArrayEnabled, out LastVertexAttribArrayEnabled[1]);
            GL.GetVertexAttrib(2, VertexAttribParameter.VertexAttribArrayEnabled, out LastVertexAttribArrayEnabled[2]);
            GL.GetVertexAttrib(3, VertexAttribParameter.VertexAttribArrayEnabled, out LastVertexAttribArrayEnabled[3]);

            LastScissorTest = GL.IsEnabled(EnableCap.ScissorTest);
            LastStencilTest = GL.IsEnabled(EnableCap.StencilTest);
            LastDepthTest = GL.IsEnabled(EnableCap.DepthTest);
            LastCullFace = GL.IsEnabled(EnableCap.CullFace);
            LastBlend = GL.IsEnabled(EnableCap.Blend);

            GL.GetInteger(GetPName.FrontFace, out LastFrontFace);

            GL.GetInteger(GetPName.ColorWritemask, LastColorMask);
            
            GL.GetInteger(GetPName.BlendSrcRgb, out LastBlending[0]);
            GL.GetInteger(GetPName.BlendDstRgb, out LastBlending[1]);
            GL.GetInteger(GetPName.BlendSrcAlpha, out LastBlending[2]);
            GL.GetInteger(GetPName.BlendDstAlpha, out LastBlending[3]);

            GL.GetInteger(GetPName.FramebufferBinding, out LastFrameBuffer);
            GL.GetInteger(GetPName.Viewport, LastViewport);
        }

        public void RestoreState()
        {
            GL.UseProgram((uint)LastProgram);

            SetEnabledVertexAttribArray(0, LastVertexAttribArrayEnabled[0] != 0);
            SetEnabledVertexAttribArray(1, LastVertexAttribArrayEnabled[1] != 0);
            SetEnabledVertexAttribArray(2, LastVertexAttribArrayEnabled[2] != 0);
            SetEnabledVertexAttribArray(3, LastVertexAttribArrayEnabled[3] != 0);

            SetEnabled(EnableCap.ScissorTest, LastScissorTest);
            SetEnabled(EnableCap.StencilTest, LastStencilTest);
            SetEnabled(EnableCap.DepthTest, LastDepthTest);
            SetEnabled(EnableCap.CullFace, LastCullFace);
            SetEnabled(EnableCap.Blend, LastBlend);
            
            GL.FrontFace((FrontFaceDirection)LastFrontFace);

            GL.ColorMask(LastColorMask[0] != 0, LastColorMask[1] != 0, LastColorMask[2] != 0, LastColorMask[3] != 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, (uint)LastArrayBufferBinding);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, (uint)LastElementArrayBufferBinding);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, (uint)LastTexture1Binding2D);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, (uint)LastTexture0Binding2D);

            GL.ActiveTexture((TextureUnit)LastActiveTexture);
            
            GL.BlendFuncSeparate((BlendingFactorSrc)LastBlending[0], (BlendingFactorDest)LastBlending[1], (BlendingFactorSrc)LastBlending[2], (BlendingFactorDest)LastBlending[3]);

            RestoreViewport();
            RestoreFrameBuffer();
        }

        public void RestoreViewport()
        {
            GL.Viewport(LastViewport[0], LastViewport[1], LastViewport[2], LastViewport[3]);
        }

        public void RestoreFrameBuffer()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)LastFrameBuffer);
        }

        private static void SetEnabled(EnableCap cap, bool enabled)
        {
            if (enabled)
                GL.Enable(cap);
            else
                GL.Disable(cap);
        }

        private static void SetEnabledVertexAttribArray(int index, bool enabled)
        {
            if (enabled)
                GL.EnableVertexAttribArray((uint)index);
            else
                GL.DisableVertexAttribArray((uint)index);
        }


    }
}