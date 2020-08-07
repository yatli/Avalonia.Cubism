using System;
using System.ComponentModel;
using System.IO;
using CubismFramework;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES20;

namespace TestForms
{
    public class Game : GameWindow
    {
        CubismAsset Asset;
        CubismRenderingManager RenderingManager;
        CubismOpenTKRenderer Renderer;
        CubismMotionQueueEntry LastMotion;
        double elapsed;

        public Game(int width, int height, string title)
            : base(width, height, GraphicsMode.Default, title)
        {    
        }

        protected override void OnLoad(EventArgs e)
        {
            Asset = new CubismAsset(@"hiyori_free_t06.model3.json", (string file_path) =>
            {
                string file_name = Path.GetFileNameWithoutExtension(file_path);
                string resource_name = file_name.Replace('.', '_');
                byte[] byte_array = (byte[])Hiyori.ResourceManager.GetObject(resource_name);
                return new MemoryStream(byte_array);
            });

            var eye_blink_controller = new CubismEyeBlink(Asset.ParameterGroups["EyeBlink"]);
            Asset.StartMotion(CubismAsset.MotionType.Effect, eye_blink_controller);

            Renderer = new CubismOpenTKRenderer();
            RenderingManager = new CubismRenderingManager(Renderer, Asset);

            base.OnLoad(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if ((LastMotion == null) || (LastMotion.Finished == true))
            {
                var motion_group = Asset.MotionGroups[""];
                int number = new Random().Next() % motion_group.Length;
                var motion = (CubismMotion)motion_group[number];
                LastMotion = Asset.StartMotion(CubismAsset.MotionType.Base, motion, false);
            }

            Asset.Update(elapsed);

            GL.ClearColor(0.0f, 0.5f, 0.5f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Viewport(0, 0, Width, Height);
            Matrix4 mvp_matrix = Matrix4.Identity;
            mvp_matrix[0, 0] = 2.0f;
            mvp_matrix[1, 1] = 2.0f * Width / Height;
            RenderingManager.Draw(mvp_matrix);

            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            elapsed = e.Time;

            base.OnUpdateFrame(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            RenderingManager.Dispose();
            Renderer.Dispose();
            Asset.Dispose();

            base.OnClosing(e);
        }
    }
}