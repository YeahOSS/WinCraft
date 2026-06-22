using System.Runtime.Serialization;
using Microsoft.Win32;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Describes a single registry value write request.
    /// </summary>
    [DataContract]
    internal sealed class RegistryValueWriteRequest
    {
        [DataMember(Order = 1)]
        public RegistryValueLocation Location { get; set; }

        [DataMember(Order = 2)]
        public string SubKeyPath { get; set; }

        [DataMember(Order = 3)]
        public string ValueName { get; set; }

        [DataMember(Order = 4)]
        public string ValueData { get; set; }

        [DataMember(Order = 5)]
        public RegistryValueKind ValueKind { get; set; }
    }
}
