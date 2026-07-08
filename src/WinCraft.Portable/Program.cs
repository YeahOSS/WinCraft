using System;

namespace WinCraft
{
    internal static class Program
    {
#if !DEBUG

        static Program()
        {
            AssemblyResolver.Register();
        }
#endif

        [STAThread]
        private static void Main(string[] args)
        {
            Startup.ProgramHost.Run(args);
        }
    }
}
