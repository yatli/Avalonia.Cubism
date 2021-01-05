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
using System.Numerics;
using Avalonia.VisualTree;

namespace Avalonia.Cubism
{
    public class Live2DControl : OpenGlControlBase
    {
        #region Dependent Properties
        public static readonly StyledProperty<CubismAsset> AssetProperty = AvaloniaProperty.Register<Live2DControl, CubismAsset>("Asset");
        public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<Live2DControl, double>("Scale");
        #endregion

        static Live2DControl()
        {
            AffectsRender<Live2DControl>(AssetProperty);
            AffectsRender<Live2DControl>(ScaleProperty);
        }

        private Stopwatch m_time = new Stopwatch();

        private CubismAvaloniaRenderer m_renderer;
        private CubismRenderingManager m_rendermgr;
        private CubismMotionQueueEntry m_lastmotion;

        private GlInterfaceEx m_gl;
        //private unsafe GlDebugProc m_debugProc;
        private int[] m_vao;

        private void UpdateAsset(CubismAsset asset)
        {
            DisposeRenderer();
            var old_asset = Asset;
            if (old_asset != null)
            {
                old_asset.Dispose();
            }

            if (asset == null)
            {
                SetValue(AssetProperty, null);
                return;
            }
            else
            {
                var eye_blink_controller = new CubismEyeBlink(asset.ParameterGroups["EyeBlink"]);
                asset.StartMotion(MotionType.Effect, eye_blink_controller);

                TryUpdateRenderer();
                SetValue(AssetProperty, asset);
            }

        }

        private void DisposeRenderer()
        {
            if (m_rendermgr != null)
            {
                m_rendermgr.Dispose();
                m_rendermgr = null;
            }
            if (m_renderer != null)
            {
                m_renderer.Dispose();
                m_renderer = null;
            }
        }

        private void TryUpdateRenderer()
        {
            var asset = GetValue(AssetProperty);
            if (asset == null)
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
            m_rendermgr = new CubismRenderingManager(m_renderer, asset);

            // !must be configured so for Avalonia
            m_renderer.UsePremultipliedAlpha = true;
        }

        private static unsafe void OnGlDebugMessage(int src, int ty, int id, int sev, int len, byte* msg, void* userparam)
        {
            var str = UTF8Encoding.UTF8.GetString(msg, len);
        }

        protected unsafe override void OnOpenGlInit(GlInterface gl, int fb)
        {
            m_gl = new GlInterfaceEx(gl);

            // hook up debug handler
            //m_debugProc = OnGlDebugMessage;
            //gl.Enable(GL_DEBUG_OUTPUT);
            //m_gl.DebugMessageCallback(m_debugProc, null);

            // allocate vertex array object (VAO)
            m_vao = new int[1];
            m_gl.GenVertexArrays(1, m_vao);
            m_gl.BindVertexArray(m_vao[0]);

            // initialize Cubism renderer
            TryUpdateRenderer();
            m_time.Restart();
        }

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            DisposeRenderer();
            m_gl.BindVertexArray(0);
            m_gl.DeleteVertexArrays(1, m_vao);

            m_vao = null;
            m_gl = null;
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (Asset == null)
            {
                return;
            }

            if ((m_lastmotion == null) || (m_lastmotion.Finished == true))
            {
                var motion_group = Asset.MotionGroups[""];
                int number = new Random().Next() % motion_group.Length;
                var motion = (CubismMotion)motion_group[number];
                m_lastmotion = Asset.StartMotion(MotionType.Base, motion, false);
            }

            Asset.Update(m_time.ElapsedMilliseconds / 1000.0);
            m_time.Restart();
            double controlScaling = VisualRoot.RenderScaling;
            int w = (int)(Bounds.Width * controlScaling);
            int h = (int)(Bounds.Height * controlScaling);
            double r = Math.Sqrt(((float)w) / h);

            gl.Viewport(0, 0, w, h);
            gl.Clear(GL_COLOR_BUFFER_BIT);

            Matrix4x4 mvp_matrix = Matrix4x4.Identity;
            if (h >= w)
            {
                mvp_matrix.M11 = 1.5f;
                mvp_matrix.M22 = -1.5f * w / h;
            }
            else
            {
                mvp_matrix.M11 = 1.5f * h / w;
                mvp_matrix.M22 = -1.5f;
            }

            m_rendermgr.Draw(mvp_matrix);

            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        public CubismAsset Asset
        {
            get => GetValue<CubismAsset>(AssetProperty);
            set => UpdateAsset(value);
        }

        public Live2DControl() : base()
        {
        }

    }
}
