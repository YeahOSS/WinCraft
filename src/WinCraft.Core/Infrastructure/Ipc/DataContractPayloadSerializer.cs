using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Serializes IPC payloads using data contracts shared by both processes.
    /// </summary>
    internal static class DataContractPayloadSerializer
    {
        public static string Serialize<T>(T value)
        {
            var serializer = new DataContractSerializer(typeof(T));

            using var stream = new MemoryStream();
            serializer.WriteObject(stream, value);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static T Deserialize<T>(string payload)
        {
            var serializer = new DataContractSerializer(typeof(T));
            var buffer = Encoding.UTF8.GetBytes(payload ?? string.Empty);

            using var stream = new MemoryStream(buffer);
            return (T)serializer.ReadObject(stream);
        }
    }
}
