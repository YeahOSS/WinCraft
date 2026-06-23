using System.Threading.Tasks;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Maps privileged host command results into UI-friendly outcomes.
    /// </summary>
    internal sealed class PrivilegeBroker(ElevatedAgentController controller) : IPrivilegeBroker
    {
        private readonly ElevatedAgentController _controller = controller;

        public PrivilegeExecutionResult Execute(ElevatedCommandRequest request)
        {
            if (_controller == null)
            {
                if (ProcessElevation.IsCurrentProcessElevated())
                {
                    var localResult = ElevatedOperationExecutor.Execute(request);
                    return MapResult(localResult);
                }

                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent controller is not available.");
            }

            var result = _controller.Execute(request);
            return MapResult(result);
        }

        /// <summary>
        /// Executes a privileged request on a background thread so the
        /// caller never blocks on a UAC prompt, pipe connection, or
        /// agent communication timeout.
        /// </summary>
        public Task<PrivilegeExecutionResult> ExecuteAsync(ElevatedCommandRequest request)
        {
            return Task.Run(() => Execute(request));
        }

        private static PrivilegeExecutionResult MapResult(CommandResult result)
        {
            if (result == null)
            {
                return PrivilegeExecutionResult.Failure(
                    PrivilegeErrorCodes.EmptyAgentResponse,
                    "The elevated agent returned no response.");
            }

            if (result.Succeeded)
                return PrivilegeExecutionResult.Success();

            if (result.ErrorCode == PrivilegeErrorCodes.AgentStartCancelled)
                return PrivilegeExecutionResult.Cancelled(result.ErrorCode, result.ErrorMessage);

            if (result.ErrorCode == PrivilegeErrorCodes.AgentUnavailable)
                return PrivilegeExecutionResult.Unavailable(result.ErrorCode, result.ErrorMessage);

            return PrivilegeExecutionResult.Failure(result.ErrorCode, result.ErrorMessage);
        }
    }
}
