// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------
using Gurux.Compression.Enums;
using Gurux.Compression.Internal;
using System.ComponentModel;
using System.Text;
using System.Buffers;
using Gurux.Compression.Properties;

namespace Gurux.Compression
{
    /// <summary>
    /// This class implements V.44 compression decoder 
    /// that is defined in ITU-T standard ITU-T V.44: 2000.
    /// </summary>
    /// <remarks>
    /// V.44 compression is based for the ITU standard:
    /// SERIES V: DATA COMMUNICATION OVER THE TELEPHONE NETWORK – Error control – 
    /// V.44:2000, Data compression procedures
    /// </remarks>
    public class GXV44Decoder
    {
        /// <summary>Validated option snapshot isolated from caller mutations.</summary>
        private readonly GXv44Options _options;

        /// <summary>Reusable storage for bytes decoded since the latest dictionary initialization.</summary>
        private readonly ArrayBufferWriter<byte> _history = new();

        /// <summary>Previous data-token kind used to apply the string-creation rules.</summary>
        private Operation _previousType = Operation.Ordinal;

        /// <summary>Codeword associated with the previous dictionary-string token.</summary>
        private UInt16 _previousCodeword = 0;

        /// <summary>Byte emitted by the previous ordinal token.</summary>
        private byte _previousOrdinal = 0;

        /// <summary>Next available dictionary codeword, starting at four.</summary>
        private UInt16 _nextCodeword;

        /// <summary>First history byte not yet included in a diagnostic value.</summary>
        private int _lastValuePosition = 0;

        /// <summary>
        /// Initializes a new instance of the GXV44Decoder class with default encoding options.
        /// </summary>
        public GXV44Decoder() : this(new GXv44Options())
        {
        }

        /// <summary>
        /// Initializes a new instance of the GXV44Decoder class with specified decoding options.
        /// </summary>
        /// <param name="options">The encoding options to use.</param>
        public GXV44Decoder(GXv44Options options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options.Snapshot();
            _strings = new GXV44Table<GXV44DecoderEntry>(_options.MaxCodewords - 4);
        }

        /// <summary>
        /// Gets the current ordinal width in bits, restored to its configured default on reinitialization.
        /// </summary>
        [DefaultValue(7)]
        public byte OrdinalSize { get; private set; } = 7;

        /// <summary>
        /// Gets the current codeword width in bits, restored to its configured default on reinitialization.
        /// </summary>
        [DefaultValue(6)]
        public byte CodewordSize { get; private set; } = 6;

        /// <summary>
        /// Stores dictionary entries and their child and sibling links.
        /// </summary>
        private readonly GXV44Table<GXV44DecoderEntry> _strings;

        /// <summary>Returns a detached, read-only snapshot of the diagnostic dictionary.</summary>
        public IReadOnlyList<GXv44DecoderEntry> StringCollection
        {
            get
            {
                var snapshot = new GXv44DecoderEntry[_strings.Count];
                for (int i = 0; i < snapshot.Length; ++i)
                {
                    snapshot[i] = _strings[i].Snapshot();
                }

                return Array.AsReadOnly(snapshot);
            }
        }

        /// <summary>Releases captured diagnostic data and formatted values, retaining dictionary metadata.</summary>
        /// <remarks>The reusable history buffer is retained. Existing detached snapshots remain valid.</remarks>
        public void ClearDiagnostics()
        {
            for (int i = 0; i < _strings.Count; ++i)
            {
                _strings[i].ClearDiagnostics();
            }
        }

        /// <summary>
        /// Optional callback receiving detached snapshots; null disables trace construction.
        /// </summary>
        public Action<GXV44TraceArgs>? OnTrace;

        /// <summary>Publishes detached diagnostic data when a trace callback is registered.</summary>
        /// <param name="args">Trace context for the current operation.</param>
        /// <param name="code">Operation identifier to report.</param>
        /// <param name="data">Token, diagnostic node or message associated with this trace event.</param>
        /// <param name="uncompressed">Current decoder history to copy into trace arguments, or null when unavailable.</param>
        /// <param name="index">Zero-based position associated with the requested field or trace event.</param>
        private void NotifyTrace(GXV44TraceArgs args, Operation code, object? data, ArrayBufferWriter<byte>? uncompressed, int index)
        {
            var trace = OnTrace;
            if (trace == null)
            {
                return;
            }

            var snapshot = new GXV44TraceArgs
            {
                Operation = code,
                Value = data,
                Index = index,
                Compressed = args.Compressed,
                Uncompressed = uncompressed?.WrittenMemory.ToArray(),
                StringCollection = StringCollection
            };
            trace(snapshot);
            if (snapshot.Stop)
            {
                throw new OperationCanceledException(Resources.Get("DecompressionStoppedTrace"));
            }
        }

        /// <summary>Reads one extension length at a bit offset without advancing the buffer's Position.</summary>
        /// <param name="arr">The bit buffer containing the encoded length.</param>
        /// <param name="pos">The starting bit offset.</param>
        /// <param name="size">The number of bits consumed.</param>
        /// <returns>The decoded extension length.</returns>
        internal byte GetStringExtensionLength(GXBitArray arr, int pos, out byte size)
        {
            ArgumentNullException.ThrowIfNull(arr);
            var reader = new GXV44ArrayBitReader(arr, pos);
            int length = GXV44ExtensionLength.Read(ref reader, _options.MaximumStringLength);
            size = (byte)(reader.Position - pos);
            return (byte)length;
        }

        /// <summary>
        /// Decompresses a binary string representation into its corresponding byte array.
        /// </summary>
        /// <param name="bitstring">A string containing a binary representation of data. The string may include formatting
        /// characters such as spaces, carriage returns, newlines, dashes, or braces, which will be ignored. Only '0'
        /// and '1' are considered valid binary characters.</param>
        /// <returns>A byte array representing the decompressed data derived from the binary string.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name = "bitstring"/> contains any characters other than '0', '1', spaces, carriage returns, newlines, or the
        /// allowed formatting characters ('-', '{', '}').</exception>
        public byte[] Decompress(string bitstring)
        {
            ArgumentNullException.ThrowIfNull(bitstring);
            StringBuilder sb = new StringBuilder();
            foreach (var it in bitstring)
            {
                if (it == '0' || it == '1')
                {
                    sb.Append(it);
                }
                else if (it != ' ' && it != '\r' && it != '\n' && it != '-' && it != '{' && it != '}')
                {
                    throw new ArgumentException(Resources.Format("InvalidBinaryCharacter", it));
                }
            }

            GXBitArray bs = new GXBitArray();
            bs.Add(sb.ToString());
            return Decompress(bs.Array());
        }

        /// <summary>Expands the next, not-yet-created codeword from the preceding ordinal or dictionary string.</summary>
        private void ProcessSpecialNextCodeword()
        {
            if (_previousType == Operation.Codeword || _previousType == Operation.StringExtension)
            {
                var prev = _strings[_previousCodeword - 4];
                for (int i = 0; i < prev.Length; i++)
                {
                    AppendHistory(_history.WrittenSpan[prev.StartPos + i]);
                }

                AppendHistory(_history.WrittenSpan[prev.StartPos]);
            }
            else if (_previousType == Operation.Ordinal)
            {
                AppendHistory(_previousOrdinal);
                AppendHistory(_previousOrdinal);
            }
            else
            {
                throw new InvalidDataException(Resources.Get("DecoderSpecialPreviousToken"));
            }
        }

        /// <summary>Appends one decoded byte while enforcing the negotiated history limit.</summary>
        /// <param name="value">Decoded byte to append to the current history.</param>
        private void AppendHistory(byte value)
        {
            if (_history.WrittenCount >= _options.HistoryLimit)
            {
                throw new InvalidDataException(Resources.Get("DecoderHistoryOverflow"));
            }

            _history.GetSpan(1)[0] = value;
            _history.Advance(1);
        }

        /// <summary>
        /// Resets the internal state of the decoder, including clearing the string collection, resetting the history buffer,
        /// and reinitializing codeword tracking variables.
        /// </summary>
        private void ResetDictionaries()
        {
            _strings.Clear();
            _history.ResetWrittenCount();
            _nextCodeword = 4;
            _previousType = Operation.None;
            _previousCodeword = 0;
            _previousOrdinal = 0;
            _lastValuePosition = 0;
        }

        /// <summary>
        /// Adds a new string of the specified length to the history.
        /// </summary>
        /// <param name="length">The length of the new string to add.</param>
        private void AddNewString(int length)
        {
            AddNewString(length, _history.WrittenCount - 1);
        }

        /// <summary>
        /// Adds a new string of the specified length to the history, using the specified last position as the source for the string data.
        /// </summary>
        /// <param name="length">The length of the new string to add.</param>
        /// <param name="lastPos">The position in the history buffer to use as the source for the string data.</param>
        private void AddNewString(int length, int lastPos)
        {
            if (length > (int)_options.MaximumStringLength || 4 + _strings.Count >= _options.MaxCodewords)
            {
                return;
            }

            GXV44DecoderEntry entry = new GXV44DecoderEntry((ushort)(4 + _strings.Count), lastPos, length);
            int valueCount = Math.Max(0, _history.WrittenCount - _lastValuePosition);
            entry.SetDiagnosticData(_history.WrittenMemory, _lastValuePosition, valueCount, _options.Hex);
            _lastValuePosition = _history.WrittenCount;
            _strings.Add(entry);
            ++_nextCodeword;
        }

        /// <summary>
        /// Creates a new string in the history based on the previous codeword and the specified extension length.
        /// </summary>
        /// <param name="extensionLength">The length of the extension to add to the previous codeword's string.</param>
        private void CreateStringAfterExtension(int extensionLength)
        {
            AddNewString(GetStringLength(_previousCodeword) + extensionLength);
        }

        /// <summary>
        /// Retrieves the length of the string associated with the specified codeword. If the codeword corresponds to the next codeword to be added,
        /// the length is determined based on the previous operation type.
        /// </summary>
        /// <param name="codeword">The codeword for which to retrieve the string length.</param>
        /// <returns>The length of the string associated with the specified codeword.</returns>
        private int GetStringLength(UInt16 codeword)
        {
            if (codeword == 4 + _strings.Count)
            {
                if (_previousType == Operation.Codeword)
                {
                    return (GetStringLength(_previousCodeword) + 1);
                }

                if (_previousType == Operation.Ordinal)
                {
                    return 2;
                }
            }

            return _strings[codeword - 4].Length;
        }

        /// <summary>
        /// Creates a new string in the history based on the previous operation type after processing an ordinal value. The length of the new string is determined
        /// by the previous operation type.
        /// </summary>
        private void CreateStringAfterOrdinal()
        {
            switch (_previousType)
            {
                case Operation.Ordinal:
                    AddNewString(2);
                    break;
                case Operation.Codeword:
                    AddNewString(GetStringLength(_previousCodeword) + 1);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Processes an ordinal value by appending it to the history and creating a new string based on the previous operation type. The previous operation type and ordinal value are updated accordingly.
        /// </summary>
        /// <param name="value">The ordinal value to process.</param>
        private void ProcessOrdinal(byte value)
        {
            AppendHistory(value);
            CreateStringAfterOrdinal();
            _previousType = Operation.Ordinal;
            _previousOrdinal = value;
        }

        /// <summary>
        /// Generates and adds a new string to the dictionary based on the current codeword and the type of the previous
        /// operation.  
        /// </summary>
        /// <param name="outputStart">The starting index in the output for the new string.</param>
        private void CreateStringAfterCodeword(int outputStart)
        {
            switch (_previousType)
            {
                case Operation.Ordinal:
                    // Table 2: Codeword after Ordinal
                    AddNewString(2, outputStart);
                    break;
                case Operation.Codeword:
                    // Table 2: Codeword after Codeword
                    AddNewString(GetStringLength(_previousCodeword) + 1, outputStart);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Copies the string associated with the specified codeword from the history to the output. The string is retrieved from the string collection based on the codeword index, and each byte of the string is appended to the history.
        /// </summary>
        /// <param name="codeword">The codeword whose associated string is to be copied.</param>
        private void CopyString(int codeword)
        {
            var entry = _strings[codeword - 4];
            for (int i = 0; i < entry.Length; i++)
            {
                AppendHistory(_history.WrittenSpan[entry.StartPos + i]);
            }
        }

        /// <summary>
        /// Processes a codeword by validating it against the next expected codeword, copying the associated string to the output if it is valid, and creating a new string based on the previous operation type. 
        /// The previous operation type and codeword are updated accordingly.
        /// </summary>
        /// <param name="codeword">The codeword to process.</param>
        /// <exception cref="InvalidOperationException">Thrown if the codeword is invalid or out of range.</exception>
        private void ProcessCodeword(UInt16 codeword)
        {
            if (codeword < 4 || codeword >= _options.MaxCodewords || codeword > _nextCodeword)
            {
                throw new InvalidDataException(Resources.Format("DecoderCodewordOutOfRange", codeword, 4 + _strings.Count));
            }

            if (codeword == _nextCodeword && (_previousType != Operation.Ordinal && _previousType != Operation.Codeword))
            {
                throw new InvalidDataException(Resources.Get("DecoderUncreatedRequiresPrevious"));
            }

            if (codeword == _nextCodeword && _previousType == Operation.Codeword && GetStringLength(_previousCodeword) >= (int)_options.MaximumStringLength)
            {
                throw new InvalidDataException(Resources.Get("DecoderUncreatedExceedsMaximum"));
            }

            int outputStart = _history.WrittenCount;
            if (codeword < 4 + this._strings.Count)
            {
                CopyString(codeword);
            }
            else
            {
                ProcessSpecialNextCodeword();
            }

            CreateStringAfterCodeword(outputStart);
            _previousType = Operation.Codeword;
            _previousCodeword = codeword;
        }

        /// <summary>
        /// Processes a string extension by validating it against the previous codeword, copying the associated string extension to the output if it is valid, and creating a new string based on the previous operation type.
        /// The previous operation type is updated accordingly.
        /// </summary>
        /// <param name="extensionLength">The length of the string extension to process.</param>
        /// <exception cref="InvalidOperationException">Thrown if the string extension is invalid or out of range.</exception>
        private void ProcessStringExtension(int extensionLength)
        {
            if (_previousType != Operation.Codeword)
            {
                throw new InvalidDataException(Resources.Get("DecoderExtensionRequiresCodeword"));
            }

            if (_strings.Count <= _previousCodeword - 4)
            {
                throw new InvalidDataException(Resources.Format("DecoderPreviousCodewordNotFound", _previousCodeword));
            }

            var entry = _strings[_previousCodeword - 4];
            int source = entry.Position + 1;
            for (int i = 0; i < extensionLength; i++)
            {
                if (source + i >= _history.WrittenCount)
                {
                    throw new InvalidDataException(Resources.Get("DecoderHistoryUnderflow"));
                }

                AppendHistory(_history.WrittenSpan[source + i]);
            }

            CreateStringAfterExtension(extensionLength);
            _previousType = Operation.StringExtension;
        }

        /// <summary>
        /// Decompresses the provided byte array using the V.44 compression algorithm.
        /// </summary>
        /// <remarks>This method processes the input byte array using the GXV44 compression algorithm to
        /// reconstruct the original data. Malformed or incomplete compressed input is rejected.</remarks>
        /// <param name="value">The compressed data as a byte array. Must not be null.</param>
        /// <returns>A byte array containing the decompressed data.</returns>
        /// <exception cref="InvalidDataException">Thrown for malformed or incomplete compressed data.</exception>
        public byte[] Decompress(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            using var input = new MemoryStream(value, writable: false);
            using var output = new MemoryStream();
            string? traceBits = null;
            if (OnTrace != null)
            {
                var bits = new GXBitArray();
                bits.Add(value);
                traceBits = bits.ToBitString();
            }

            DecompressCore(input, output, traceBits);
            return output.ToArray();
        }

        /// <summary>Decompresses a complete V.44 stream, leaving both streams open.</summary>
        /// <remarks>Completed history blocks may be written before a later malformed token is detected.</remarks>
        /// <param name="input">Source bytes or readable stream to process.</param>
        /// <param name="output">Writable destination that remains open after processing.</param>
        /// <exception cref="ArgumentNullException">An input or output stream is null.</exception>
        /// <exception cref="ArgumentException">The input is unreadable, the output is unwritable, or both streams are the same instance.</exception>
        /// <exception cref="IOException">Reading or writing a stream fails.</exception>
        /// <exception cref="InvalidDataException">The compressed data is malformed or incomplete.</exception>
        public void Decompress(Stream input, Stream output) => DecompressCore(input, output, null);

        /// <summary>Reads complete compressed tokens, writes decoded blocks, and preserves caller-owned streams.</summary>
        /// <param name="input">Source bytes or readable stream to process.</param>
        /// <param name="output">Writable destination that remains open after processing.</param>
        /// <param name="traceBits">Complete compressed input as a contiguous bit string, or null for streaming input.</param>
        private void DecompressCore(Stream input, Stream output, string? traceBits)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(output);
            if (!input.CanRead)
            {
                throw new ArgumentException(Resources.Get("ReadableStreamRequired"), nameof(input));
            }

            if (!output.CanWrite)
            {
                throw new ArgumentException(Resources.Get("WritableStreamRequired"), nameof(output));
            }

            if (ReferenceEquals(input, output))
            {
                throw new ArgumentException(Resources.Get("DifferentStreamsRequired"));
            }

            ResetDictionaries();
            OrdinalSize = (byte)_options.DefaultOrdinalSize;
            CodewordSize = (byte)_options.DefaultCodewordSize;
            var reader = new GXV44BitReader(input);
            var args = new GXV44TraceArgs
            {
                Compressed = traceBits
            };
            NotifyTrace(args, Operation.Start, null, null, 0);
            bool afterCodeword = false;
            bool endedWithFlush = false;
            int written = 0;
            try
            {
                while (reader.TryPeek(out _))
                {
                    endedWithFlush = false;
                    int prefix = reader.Read(1);
                    if (afterCodeword && prefix == 0)
                    {
                        int second = reader.Read(1);
                        if (second == 1)
                        {
                            int extension = GXV44ExtensionLength.Read(ref reader, _options.MaximumStringLength);
                            if (GetStringLength(_previousCodeword) + extension > (int)_options.MaximumStringLength)
                            {
                                throw new InvalidDataException(Resources.Get("DecoderExtendedExceedsMaximum"));
                            }

                            ProcessStringExtension(extension);
                            afterCodeword = false;
                            continue;
                        }
                    }

                    afterCodeword = false;
                    if (prefix == 0)
                    {
                        ProcessOrdinal((byte)reader.Read(OrdinalSize));
                        continue;
                    }

                    ushort code = (ushort)reader.Read(CodewordSize);
                    switch (code)
                    {
                        case 0:
                            throw new InvalidDataException(Resources.Get("DecoderUnsupportedTransparent"));
                        case 1:
                            reader.Align();
                            output.Write(_history.WrittenSpan[written..]);
                            written = _history.WrittenCount;
                            NotifyTrace(args, Operation.Flush, null, _history, TraceIndex(reader));
                            endedWithFlush = true;
                            break;
                        case 2:
                            if (!reader.TryPeek(out int nextPrefix))
                            {
                                throw new InvalidDataException(Resources.Get("DecoderStepUpMissingToken"));
                            }

                            if (nextPrefix == 0)
                            {
                                if (OrdinalSize == 8)
                                {
                                    throw new InvalidDataException(Resources.Get("DecoderOrdinalWidthExceeded"));
                                }

                                ++OrdinalSize;
                                NotifyTrace(args, Operation.StepUpOrdinal, null, _history, TraceIndex(reader));
                            }
                            else
                            {
                                int maxBits = System.Numerics.BitOperations.Log2((uint)(_options.MaxCodewords - 1)) + 1;
                                if (CodewordSize >= Math.Max(_options.DefaultCodewordSize, maxBits))
                                {
                                    throw new InvalidDataException(Resources.Get("DecoderCodewordWidthExceeded"));
                                }

                                ++CodewordSize;
                                NotifyTrace(args, Operation.StepUpCodeword, null, _history, TraceIndex(reader));
                            }

                            break;
                        case 3:
                            output.Write(_history.WrittenSpan[written..]);
                            written = 0;
                            ResetDictionaries();
                            OrdinalSize = (byte)_options.DefaultOrdinalSize;
                            CodewordSize = (byte)_options.DefaultCodewordSize;
                            NotifyTrace(args, Operation.ReInit, null, _history, TraceIndex(reader));
                            break;
                        default:
                            ProcessCodeword(code);
                            afterCodeword = true;
                            break;
                    }
                }

                if (!endedWithFlush)
                {
                    throw new InvalidDataException(Resources.Get("DecoderRequiresFlush"));
                }
            }
            catch (InvalidDataException ex)
            {
                NotifyTrace(args, Operation.Error, ex.Message, _history, TraceIndex(reader));
                throw;
            }

            NotifyTrace(args, Operation.End, Resources.Get("DecompressionEnded"), _history, TraceIndex(reader));
        }

        /// <summary>Clamps the compressed bit position to the integer range of trace arguments.</summary>
        /// <param name="reader">Reader tracking the current compressed bit position.</param>
        /// <returns>The current bit position, capped at Int32.MaxValue.</returns>
        private static int TraceIndex(GXV44BitReader reader) => (int)Math.Min(int.MaxValue, reader.Position);
    }
}
