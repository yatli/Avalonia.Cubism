using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new Form1());

            using (Game game = new Game(800, 600, "LearnOpenTK"))
                game.Run(60.0);
        }
    }
}
