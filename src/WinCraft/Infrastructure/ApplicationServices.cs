using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Exposes shared application services to the UI process.
    /// </summary>
    internal static class ApplicationServices
    {
        public static IPrivilegeBroker PrivilegeBroker { get; set; }

        public static PrivilegedRegistryWriter RegistryWriter { get; set; }
    }
}
