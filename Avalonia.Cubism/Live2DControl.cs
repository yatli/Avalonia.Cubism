using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using static Avalonia.OpenGL.GlConsts;

using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Threading;
using System.Diagnostics;
using CubismFramework;
using System.IO;
using Avalonia.Cubism.Render;

namespace Avalonia.Cubism
{
    public class Live2DControl : OpenGlControlBase
    {
        private Stopwatch m_time = new Stopwatch();

        private CubismAsset m_asset = null;
        private ICubismRenderer m_renderer;
        private CubismRenderingManager m_rendermgr;

        private GlInterface m_gl;

        private void UpdateAsset(CubismAsset asset)
        {
            DisposeRenderer();
            if (m_asset != null)
            {
                m_asset.Dispose();
                m_asset = null;
            }

            if (asset == null)
            {
                return;
            }

            m_asset = asset;
            var eye_blink_controller = new CubismEyeBlink(m_asset.ParameterGroups["EyeBlink"]);
            m_asset.StartMotion(MotionType.Effect, eye_blink_controller);

            TryUpdateRenderer();
        }

        private void DisposeRenderer()
        {
            if (m_rendermgr != null)
            {
                m_rendermgr.Dispose();
                m_rendermgr = null;
            }
            m_renderer = null;
        }

        private void TryUpdateRenderer()
        {
            if (m_asset == null)
            {
                return;
            }
            if (m_gl == null)
            {
                return;
            }
            if (m_renderer != null)
            {
                return;
            }

            m_renderer = new CubismAvaloniaRenderer(m_gl);
            m_rendermgr = new CubismRenderingManager(m_renderer, m_asset);
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            m_time.Restart();
            m_gl = gl;
            TryUpdateRenderer();
        }

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            DisposeRenderer();
            m_gl = null;
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        public CubismAsset Asset
        {
            get => m_asset;
            set => UpdateAsset(value);
        }

        public Live2DControl() : base()
        {
        }

        private void CheckError(GlInterface gl)
        {
            int err;
            while ((err = gl.GetError()) != GL_NO_ERROR)
                Console.WriteLine(err);
        }

    }
}
