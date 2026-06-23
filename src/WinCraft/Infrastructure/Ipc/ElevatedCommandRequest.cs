using System.Runtime.Serialization;
using WinCraft.Infrastructure.Security;

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

        [DataMember(Order = 3)]
        public string RequestId { get; set; }

        [DataMember(Order = 4)]
        public PrivilegeLevel PrivilegeLevel { get; set; }
    }
}
