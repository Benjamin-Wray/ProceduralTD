using System;

namespace ProceduralTD
{
    public static class Program
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