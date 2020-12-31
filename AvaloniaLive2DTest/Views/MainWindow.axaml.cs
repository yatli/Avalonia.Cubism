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
            this.FindControl<Live2DControl>("Live2D").Asset =
                new CubismAsset(@"hiyori_free_t06.model3.json", (string file_path) =>
                    {
                        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                        var tgt = assets.Open(new Uri($"avares://AvaloniaLive2DTest/Assets/Live2D/{file_path}"));
                        Console.WriteLine($"asset load successful: {file_path}");
                        return tgt;
                    });
        }
    }
}
