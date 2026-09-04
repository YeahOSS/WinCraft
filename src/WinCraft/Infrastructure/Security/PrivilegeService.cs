using System;
using System.Threading.Tasks;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Provides safe UI-facing probes for the privileged-agent connection.
    /// </summary>
    public static class PrivilegeService
    {
        public static Task<PrivilegeProbeResult> CheckAdministratorAsync()
        {
            return ApplicationServices.ExecuteAsync(new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                PrivilegeLevel = PrivilegeLevel.Administrator,
                RequestId = Guid.NewGuid().ToString("N")
            }).ContinueWith(task =>
            {
                if (task.Status != TaskStatus.RanToCompletion || task.Result == null)
                    return PrivilegeProbeResult.Failure("The administrator agent did not return a result.");

                var result = task.Result;
                if (result.Succeeded)
                    return PrivilegeProbeResult.Success();
                if (result.Status == PrivilegeExecutionStatus.Cancelled)
                    return PrivilegeProbeResult.Cancelled(result.ErrorMessage);

                return PrivilegeProbeResult.Failure(result.ErrorMessage);
            }, TaskScheduler.Default);
        }
    }

    public sealed class PrivilegeProbeResult
    {
        public bool Succeeded { get; private set; }

        public bool IsCancelled { get; private set; }

        public string ErrorMessage { get; private set; }

        internal static PrivilegeProbeResult Success() =>
            new PrivilegeProbeResult { Succeeded = true };

        internal static PrivilegeProbeResult Cancelled(string errorMessage) =>
            new PrivilegeProbeResult
            {
                IsCancelled = true,
                ErrorMessage = errorMessage
            };

        internal static PrivilegeProbeResult Failure(string errorMessage) =>
            new PrivilegeProbeResult { ErrorMessage = errorMessage };
    }
}
