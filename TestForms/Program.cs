using System;

namespace TestForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (Game game = new Game(400, 600, "CubismFrameworkCS Test"))
                game.Run(60.0);
        }
    }
}
