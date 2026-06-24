using System.Runtime.Serialization;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Represents the outcome of an elevated command execution.
    /// </summary>
    [DataContract]
    public sealed class CommandResult
    {
        [DataMember(Order = 1)]
        public bool Succeeded { get; set; }

        [DataMember(Order = 2)]
        public string ErrorCode { get; set; }

        [DataMember(Order = 3)]
        public string ErrorMessage { get; set; }

        [DataMember(Order = 4)]
        public string RequestId { get; set; }

        public static CommandResult Success(string requestId = null)
        {
            return new CommandResult
            {
                Succeeded = true,
                RequestId = requestId
            };
        }

        public static CommandResult Failure(string errorCode, string errorMessage, string requestId = null)
        {
            return new CommandResult
            {
                Succeeded = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                RequestId = requestId
            };
        }
    }
}
