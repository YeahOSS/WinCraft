using System.Threading.Tasks;
using WinCraft.Infrastructure.Ipc;
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

        /// <summary>
        /// Executes a privileged operation on a background thread.
        /// Safe to call from the UI thread - does not block on UAC
        /// prompts, pipe connection, or agent communication.
        /// </summary>
        public static Task<PrivilegeExecutionResult> ExecuteAsync(ElevatedCommandRequest request)
        {
            var broker = PrivilegeBroker;
            if (broker == null)
            {
                return Task.Run(() =>
                    PrivilegeExecutionResult.Unavailable(
                        PrivilegeErrorCodes.ElevatedAgentUnavailable,
                        "The elevated agent controller is not available."));
            }

            return broker.ExecuteAsync(request);
        }
    }
}
