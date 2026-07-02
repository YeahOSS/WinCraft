using System;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace WinCraft.Lzma
{
    /// <summary>
    /// Compresses and decompresses data using the vendored LZMA SDK.
    /// </summary>
    public static class LzmaCodec
    {
        private const int PropertiesLength = 5;
        private const int HeaderLength = PropertiesLength + sizeof(long);
        private const int DefaultDictionarySize = 1 << 23;
        private const int DefaultPositionStateBits = 2;
        private const int DefaultLiteralContextBits = 3;
        private const int DefaultLiteralPositionBits = 0;
        private const int DefaultAlgorithm = 2;
        private const int DefaultFastBytes = 128;
        private const string DefaultMatchFinder = "bt4";

        /// <summary>
        /// Compresses a byte array into the WinCraft LZMA container format.
        /// </summary>
        public static byte[] Compress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();
            Compress(inputStream, outputStream);
            return outputStream.ToArray();
        }

        /// <summary>
        /// Compresses a stream into the WinCraft LZMA container format.
        /// </summary>
        public static void Compress(Stream input, Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (!input.CanRead)
                throw new ArgumentException("Input stream must be readable.", nameof(input));
            if (!output.CanWrite)
                throw new ArgumentException("Output stream must be writable.", nameof(output));

            var source = PrepareInput(input, out long inputLength, out bool disposeInput);

            try
            {
                var encoder = CreateEncoder();
                encoder.WriteCoderProperties(output);
                WriteInt64LittleEndian(output, inputLength);
                encoder.Code(source, output, inputLength, -1, null);
            }
            finally
            {
                if (disposeInput)
                    source.Dispose();
            }
        }

        /// <summary>
        /// Decompresses a byte array from the WinCraft LZMA container format.
        /// </summary>
        public static byte[] Decompress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var outputLength = GetDecompressedLength(input);
            if (outputLength > int.MaxValue)
                throw new InvalidDataException("The decompressed data is too large for a byte array.");

            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream((int)outputLength);
            Decompress(inputStream, outputStream);
            if (outputStream.Length != outputLength)
                throw new InvalidDataException("The decompressed LZMA data length does not match the declared length.");
            return outputStream.ToArray();
        }

        /// <summary>
        /// Decompresses a stream from the WinCraft LZMA container format.
        /// </summary>
        public static void Decompress(Stream input, Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (!input.CanRead)
                throw new ArgumentException("Input stream must be readable.", nameof(input));
            if (!output.CanWrite)
                throw new ArgumentException("Output stream must be writable.", nameof(output));

            var properties = new byte[PropertiesLength];
            ReadExact(input, properties, 0, properties.Length);
            var outputLength = ReadInt64LittleEndian(input);
            if (outputLength < 0)
                throw new InvalidDataException("The decompressed length cannot be negative.");

            var compressedLength = input.CanSeek ? input.Length - input.Position : -1;
            var decoder = new Decoder();
            try
            {
                decoder.SetDecoderProperties(properties);
                decoder.Code(input, output, compressedLength, outputLength, null);
            }
            catch (ApplicationException ex)
            {
                throw new InvalidDataException("The LZMA payload is invalid.", ex);
            }
        }

        /// <summary>
        /// Reads the decompressed byte count from a WinCraft LZMA payload.
        /// </summary>
        public static long GetDecompressedLength(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.Length < HeaderLength)
                throw new InvalidDataException("The LZMA payload header is incomplete.");

            var value = 0L;
            for (var i = 0; i < sizeof(long); i++)
                value |= (long)input[PropertiesLength + i] << (8 * i);

            if (value < 0)
                throw new InvalidDataException("The decompressed length cannot be negative.");

            return value;
        }

        private static Encoder CreateEncoder()
        {
            var encoder = new Encoder();
            encoder.SetCoderProperties(
                [
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                ],
                [
                    DefaultDictionarySize,
                    DefaultPositionStateBits,
                    DefaultLiteralContextBits,
                    DefaultLiteralPositionBits,
                    DefaultAlgorithm,
                    DefaultFastBytes,
                    DefaultMatchFinder,
                    false
                ]);

            return encoder;
        }

        private static Stream PrepareInput(Stream input, out long length, out bool disposeInput)
        {
            if (input.CanSeek)
            {
                length = input.Length - input.Position;
                if (length < 0)
                    throw new InvalidOperationException("Input stream position is beyond the end of the stream.");

                disposeInput = false;
                return input;
            }

            var memory = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                memory.Write(buffer, 0, read);

            memory.Position = 0;
            length = memory.Length;
            disposeInput = true;
            return memory;
        }

        private static void WriteInt64LittleEndian(Stream output, long value)
        {
            var bytes = new byte[sizeof(long)];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((ulong)value >> (8 * i));

            output.Write(bytes, 0, bytes.Length);
        }

        private static long ReadInt64LittleEndian(Stream input)
        {
            var bytes = new byte[sizeof(long)];
            ReadExact(input, bytes, 0, bytes.Length);

            var value = 0L;
            for (var i = 0; i < bytes.Length; i++)
                value |= (long)bytes[i] << (8 * i);

            return value;
        }

        private static void ReadExact(Stream input, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var read = input.Read(buffer, offset, count);
                if (read == 0)
                    throw new InvalidDataException("The LZMA payload ended before the expected data was available.");

                offset += read;
                count -= read;
            }
        }
    }
}
