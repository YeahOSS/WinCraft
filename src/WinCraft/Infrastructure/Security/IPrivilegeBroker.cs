using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Executes requests that may require an elevated agent.
    /// </summary>
    internal interface IPrivilegeBroker
    {
        PrivilegeExecutionResult Execute(ElevatedCommandRequest request);
    }
}
