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

        public static CommandResult Success()
        {
            return new CommandResult { Succeeded = true };
        }

        public static CommandResult Failure(string errorCode, string errorMessage)
        {
            return new CommandResult
            {
                Succeeded = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
