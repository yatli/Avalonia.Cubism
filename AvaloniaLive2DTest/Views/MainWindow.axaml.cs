using Avalonia;
using Avalonia.Controls;
using Avalonia.Cubism;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

using CubismFramework;

using System;
using System.IO;

namespace AvaloniaLive2DTest.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // see: http://sites.cybernoids.jp/cubism_e/modeler/models/multi-arms/change-arms
            var asset =
            //new CubismAsset(@"avares://AvaloniaLive2DTest/Assets/Live2D/hiyori_free_t06.model3.json");
            new CubismAsset(@"C:\Users\Yatao\Downloads\TouhouCannonballDump\Live2D\Alice\object_live2d_014_101.asset\object_live2d_014_101.asset.model3.json");
            //new CubismAsset(@"C:\Users\Yatao\Downloads\TouhouCannonballDump\Live2D\Reimu\object_live2d_001_101.asset\object_live2d_001_101.asset.model3.json");

            // for RIM
            //asset.Model.PartGroupSwitch("PARTS_LEFT_HAND", "02");
            //asset.Model.PartGroupSwitch("PARTS_RIGHT_HAND", "02");

            // for ALS
            asset.Model.PartGroupSwitch("PartLeftHand", "02");
            asset.Model.PartGroupSwitch("PartRightHand", "02");

            this.FindControl<Live2DControl>("Live2D").Asset = asset;

            var model = asset.Model;
            var pcount = model.PartCount;
            Console.WriteLine($"part count = {pcount}");
            for(int i = 0; i < pcount; ++i)
            {
                var part = model.GetPart(i);
                Console.WriteLine($"part #{i}: {part.Name}");
            }
        }
    }
}
