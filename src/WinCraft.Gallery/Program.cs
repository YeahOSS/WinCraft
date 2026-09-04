using System;
using WinCraft.Startup;

namespace WinCraft.Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        ProgramHost.Run(args, GalleryStartup.Run);
}
