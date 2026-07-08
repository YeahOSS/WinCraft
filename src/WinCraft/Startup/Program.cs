using System;

namespace WinCraft.Startup
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            ProgramHost.Run(args);
        }
    }
}
