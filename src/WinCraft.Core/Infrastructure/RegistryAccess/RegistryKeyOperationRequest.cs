using System.Runtime.Serialization;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Payload for elevated registry key operations sent over the IPC pipe.
    /// </summary>
    [DataContract]
    internal sealed class RegistryKeyOperationRequest
    {
        [DataMember]
        public RegistryValueLocation Location { get; set; }

        [DataMember]
        public string SourceSubKeyPath { get; set; }

        [DataMember]
        public string DestinationSubKeyPath { get; set; }

        [DataMember]
        public bool Recursive { get; set; }
    }
}
