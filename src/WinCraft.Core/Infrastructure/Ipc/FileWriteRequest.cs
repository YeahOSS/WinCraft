using System.Runtime.Serialization;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Payload for an elevated file-write operation sent over the IPC pipe.
    /// </summary>
    [DataContract]
    internal sealed class FileWriteRequest
    {
        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public byte[] Content { get; set; }
    }
}
