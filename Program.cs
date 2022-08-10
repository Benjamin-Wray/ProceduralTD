using System;

namespace ProceduralTD
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Main();
            game.Run();
        }
    }
}