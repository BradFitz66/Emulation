using System;

namespace Emulation
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new GameClass())
                game.Run();
        }
    }
}
