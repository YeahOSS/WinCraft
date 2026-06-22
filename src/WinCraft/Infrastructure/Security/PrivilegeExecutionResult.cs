namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Captures the outcome of a privileged or local configuration change.
    /// </summary>
    internal sealed class PrivilegeExecutionResult
    {
        public PrivilegeExecutionStatus Status { get; private set; }

        public bool Succeeded
        {
            get { return Status == PrivilegeExecutionStatus.Succeeded; }
        }

        public string ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public static PrivilegeExecutionResult Success()
        {
            return new PrivilegeExecutionResult { Status = PrivilegeExecutionStatus.Succeeded };
        }

        public static PrivilegeExecutionResult Cancelled(string errorCode, string errorMessage)
        {
            return new PrivilegeExecutionResult
            {
                Status = PrivilegeExecutionStatus.Cancelled,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static PrivilegeExecutionResult Unavailable(string errorCode, string errorMessage)
        {
            return new PrivilegeExecutionResult
            {
                Status = PrivilegeExecutionStatus.Unavailable,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static PrivilegeExecutionResult Failure(string errorCode, string errorMessage)
        {
            return new PrivilegeExecutionResult
            {
                Status = PrivilegeExecutionStatus.Failed,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
