using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Maps elevated agent command results into UI-friendly privilege outcomes.
    /// </summary>
    internal sealed class PrivilegeBroker(ElevatedAgentController controller) : IPrivilegeBroker
    {
        private readonly ElevatedAgentController _controller = controller;

        public PrivilegeExecutionResult Execute(ElevatedCommandRequest request)
        {
            if (_controller != null)
            {
                var result = _controller.Execute(request);
                return MapResult(result);
            }

            // When the process is already running elevated there is no need
            // for an agent — execute the operation locally in the current process.
            if (ProcessElevation.IsCurrentProcessElevated())
            {
                var localResult = ElevatedOperationExecutor.Execute(request);
                return MapResult(localResult);
            }

            return PrivilegeExecutionResult.Unavailable(
                PrivilegeErrorCodes.ElevatedAgentUnavailable,
                "The elevated agent controller is not available.");
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
