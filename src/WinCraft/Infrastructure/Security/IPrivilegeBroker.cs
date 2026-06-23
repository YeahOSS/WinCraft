using System.Threading.Tasks;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Executes requests that may require an elevated agent.
    /// </summary>
    internal interface IPrivilegeBroker
    {
        /// <summary>
        /// Executes a privileged request synchronously.
        /// Call from a background thread to avoid blocking the UI.
        /// Prefer <see cref="ExecuteAsync"/> for UI-initiated calls.
        /// </summary>
        PrivilegeExecutionResult Execute(ElevatedCommandRequest request);

        /// <summary>
        /// Executes a privileged request on a background thread.
        /// Safe to call from the UI thread — the caller will not block
        /// on UAC prompts, pipe connection, or agent communication.
        /// </summary>
        Task<PrivilegeExecutionResult> ExecuteAsync(ElevatedCommandRequest request);
    }
}
