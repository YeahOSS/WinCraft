#if NET45
using System.IO;
using System.Runtime;
#endif

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Enables multicore JIT and profile-guided startup optimization on
    /// .NET 4.5+; no-op on .NET 3.0 where <see cref="ProfileOptimization"/>
    /// is unavailable.
    /// </summary>
    internal static class JitCompat
    {
        internal static void ConfigureMulticoreJit(string profileDir, string profileName)
        {
#if NET45
            try
            {
                Directory.CreateDirectory(profileDir);
                ProfileOptimization.SetProfileRoot(profileDir);
                ProfileOptimization.StartProfile(profileName);
            }
            catch
            {
                // Multicore JIT is a best-effort optimization.
            }
#endif
        }
    }
}
