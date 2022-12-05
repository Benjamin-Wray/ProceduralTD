using System;

namespace ProceduralTD
{
    internal static class Program
    {
        internal static Main Game;
        
        [STAThread]
        private static void Main()
        {
            Game = new Main();
            Game.Run();
        }
    }
}