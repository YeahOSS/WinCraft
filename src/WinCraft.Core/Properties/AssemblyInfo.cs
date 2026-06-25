using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(nameof(WinCraft))]

#if DEBUG || INSTALLER
[assembly: AssemblyTitle("WinCraft Core")]
#endif
