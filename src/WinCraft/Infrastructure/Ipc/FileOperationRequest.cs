using System.Runtime.Serialization;
using System.IO;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Payload for elevated file-system operations sent over the IPC pipe.
    /// </summary>
    [DataContract]
    internal sealed class FileOperationRequest
    {
        [DataMember]
        public string SourcePath { get; set; }

        [DataMember]
        public string DestinationPath { get; set; }

        [DataMember]
        public bool Recursive { get; set; }

        [DataMember]
        public FileAttributes Attributes { get; set; }
    }
}
