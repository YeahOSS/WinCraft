using System.Runtime.Serialization;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Defines a single elevated operation request.
    /// </summary>
    [DataContract]
    public sealed class ElevatedCommandRequest
    {
        [DataMember(Order = 1)]
        public string OperationName { get; set; }

        [DataMember(Order = 2)]
        public string Payload { get; set; }
    }
}
