using System;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Shared typed-message read/write over a named pipe using
    /// length-prefixed DataContract serialization. Used by both the
    /// UI-side pipe server and the elevated-agent pipe client.
    /// </summary>
    internal static class PipeMessageIO
    {
        /// <summary>
        /// Maximum payload length (1 MB) to guard against corrupted or
        /// malicious length prefixes that could cause an out-of-memory condition.
        /// </summary>
        private const int MaxPayloadLength = 1024 * 1024;

        /// <summary>
        /// Writes a typed message to the pipe: 4-byte length prefix
        /// followed by the UTF-8 serialized payload.
        /// </summary>
        /// <remarks>
        /// Each <see cref="PipeBufferIO.WriteBuffer"/> call waits for the
        /// overlapped write to complete via <c>GetOverlappedResult</c>, so
        /// an explicit <c>FlushFileBuffers</c> is not needed.
        /// </remarks>
        public static void WriteMessage<T>(SafeFileHandle pipeHandle, T message)
        {
            var serialized = DataContractPayloadSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(serialized);
            PipeBufferIO.WriteBuffer(pipeHandle, BitConverter.GetBytes(buffer.Length));
            PipeBufferIO.WriteBuffer(pipeHandle, buffer);
        }

        /// <summary>
        /// Reads a typed message from the pipe: 4-byte length prefix
        /// followed by the UTF-8 serialized payload.
        /// </summary>
        public static T ReadMessage<T>(SafeFileHandle pipeHandle, string brokenPipeMessage)
        {
            var lengthBuffer = PipeBufferIO.ReadExact(pipeHandle, sizeof(int), brokenPipeMessage);
            var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (payloadLength < 0 || payloadLength > MaxPayloadLength)
                throw new InvalidOperationException("The pipe peer sent an invalid payload length.");

            var payloadBuffer = PipeBufferIO.ReadExact(pipeHandle, payloadLength, brokenPipeMessage);
            var serialized = Encoding.UTF8.GetString(payloadBuffer);
            return DataContractPayloadSerializer.Deserialize<T>(serialized);
        }
    }
}
