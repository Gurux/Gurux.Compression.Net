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
using Gurux.Compression.Properties;

namespace Gurux.Compression
{
    /// <summary>
    /// This class implements V.44 compression encoder that is defined in ITU-T standard ITU-T V.44: 2000.
    /// </summary>
    /// <remarks>
    /// V.44 compression is based for the ITU standard:
    /// SERIES V: DATA COMMUNICATION OVER THE TELEPHONE NETWORK – Error control – 
    /// V.44:2000, Data compression procedures
    /// </remarks>
    public class GXV44Encoder
    {
        /// <summary>Validated option snapshot used independently of later caller changes.</summary>
        private readonly GXv44Options _options;

        /// <summary>Position counters owned by one CompressBlock call.</summary>
        private struct EncodingState
        {
            // Absolute index of the next history byte in the current input array.

            /// <summary>Absolute input index of the next history byte in the current block.</summary>
            public UInt32 History;

            /// <summary>Absolute input index at which the current dictionary history begins.</summary>
            public int HistoryStart;

            /// <summary>Exclusive input boundary imposed by the current history window.</summary>
            public int InputEnd;
        }

        /// <summary>
        /// Initializes a new instance of the GXV44Encoder class with default encoding options.
        /// </summary>
        public GXV44Encoder() : this(new GXv44Options())
        {
        }

        /// <summary>
        /// Initializes a new instance of the GXV44Encoder class with specified encoding options.
        /// </summary>
        /// <param name="options">The encoding options to use.</param>
        public GXV44Encoder(GXv44Options options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options.Snapshot();
            _nodes = new GXV44Table<GXV44EncoderEntry>(_options.MaxCodewords - 4);
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
        /// Maps each root byte to its first dictionary entry.
        /// </summary>
        private readonly UInt16[] _rootArray = new UInt16[256];

        /// <summary>
        /// Returns root array with down-index.
        /// </summary>
        /// <returns>Detached root-byte and dictionary-codeword pairs for roots with valid links.</returns>
        public List<KeyValuePair<byte, UInt16>> GetRootArray()
        {
            byte index = 0;
            List<KeyValuePair<byte, UInt16>> list = [];
            foreach (var it in _rootArray)
            {
                if (it != 0)
                {
                    list.Add(new KeyValuePair<byte, UInt16>(index, _nodes[it - 4].Codeword));
                }
                ++index;
            }
            return list;
        }

        /// <summary>
        /// Stores dictionary entries and their child and sibling links.
        /// </summary>
        private readonly GXV44Table<GXV44EncoderEntry> _nodes;

        /// <summary>Returns a detached, read-only snapshot of the diagnostic dictionary.</summary>
        public IReadOnlyList<GXv44Node> NodeTree
        {
            get
            {
                var snapshot = new GXv44Node[_nodes.Count];
                for (int i = 0; i < snapshot.Length; ++i)
                {
                    snapshot[i] = _nodes[i].Snapshot();
                }

                return Array.AsReadOnly(snapshot);
            }
        }

        /// <summary>Releases captured diagnostic data and formatted values, retaining dictionary metadata.</summary>
        /// <remarks>Call after processing. Existing detached snapshots remain valid.</remarks>
        public void ClearDiagnostics()
        {
            for (int i = 0; i < _nodes.Count; ++i)
            {
                _nodes[i].ClearDiagnostics();
            }
        }

        /// <summary>
        /// Represents a compressed bit array used for internal data storage or manipulation.
        /// </summary>
        /// <remarks>This field is intended for internal use and stores a compressed representation of a
        /// bit array. It is not exposed publicly and should not be accessed directly outside of the containing
        /// class.</remarks>
        private readonly GXBitArray _compressed = new();

        /// <summary>
        /// Optional callback receiving detached snapshots; null disables trace construction.
        /// </summary>
        public Action<GXV44TraceArgs>? OnTrace;

        /// <summary>
        /// Adds a string extension length to the specified <see cref = "GXBitArray"/> based on the provided value.
        /// </summary>
        /// <remarks>The method encodes the specified length value into the <see cref = "GXBitArray"/> using
        /// a predefined mapping or encoding logic. The encoding varies depending on the value of <paramref 
        ///name = "value"/>.</remarks>
        /// <param name="arr">The <see cref="GXBitArray"/> to which the string extension length will be added.</param>
        /// <param name="value">The length value to add. Must be between 1 and 253, inclusive.  Values outside this range will result in an
        /// exception.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is greater than 253.</exception>
        internal void AddStringExtensionLength(GXBitArray arr, int value)
        {
            ArgumentNullException.ThrowIfNull(arr);
            if (value < 1 || value > (int)_options.MaximumStringLength - 2)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            switch (value)
            {
                case 1:
                    arr.Add(1, 1);
                    break;
                case 2:
                    arr.Add(2, 3);
                    break;
                case 3:
                    arr.Add(0, 1);
                    arr.Add(2, 2);
                    break;
                case 4:
                    arr.Add(0, 1);
                    arr.Add(3, 2);
                    break;
                case 5:
                    arr.Add(0, 7);
                    break;
                case 6:
                    arr.Add(0x10, 7);
                    break;
                case 7:
                    arr.Add(0x20, 7);
                    break;
                case 8:
                    arr.Add(0x30, 7);
                    break;
                case 9:
                    arr.Add(0x40, 7);
                    break;
                case 10:
                    arr.Add(0x50, 7);
                    break;
                case 11:
                    arr.Add(0x60, 7);
                    break;
                case 12:
                    arr.Add(0x70, 7);
                    break;
                default:
                    if (_options.MaximumStringLength == MaximumStringLength.Value46)
                    {
                        arr.Add(0, 1);
                        arr.Add(0, 2);
                        arr.Add(1, 1);
                        arr.Add(value - 13, 5);
                    }
                    else if (_options.MaximumStringLength == MaximumStringLength.Value78)
                    {
                        arr.Add(0, 1);
                        arr.Add(0, 2);
                        arr.Add(1, 1);
                        arr.Add(value - 13, 6);
                    }
                    else if (_options.MaximumStringLength == MaximumStringLength.Value142)
                    {
                        arr.Add(0, 1);
                        arr.Add(0, 2);
                        arr.Add(1, 1);
                        arr.Add(value - 13, 7);
                    }
                    else
                    {
                        arr.Add(0, 1);
                        arr.Add(0, 2);
                        arr.Add(1, 1);
                        arr.Add(value - 13, 8);
                    }

                    break;
            }
        }

        /// <summary>
        /// Writes an ordinal value to the compressed output, adjusting the encoding size if necessary.
        /// </summary>
        /// <remarks>If the value of <paramref name = "ch"/> exceeds the default ordinal size, the method
        /// transitions  to a larger encoding size and writes the value using the updated size. Otherwise, the value is 
        /// written using the current ordinal size. This method ensures that the compressed output adapts  dynamically
        /// to accommodate larger values.</remarks>
        /// <param name="ch">The ordinal value to write. Must be a byte value.</param>
        /// <param name="previous">Previous token kind, used to select the correct ordinal prefix.</param>
        private void WriteOrdinal(byte ch, Operation previous)
        {
            //STEPUP is added if value is bigger than OrdinalSize.
            int bits = ch > 127 ? 8 : 7;
            if (bits > OrdinalSize)
            {
                //Move to StepUp
                _compressed.Add(1, 1);
                _compressed.Add(2, CodewordSize);
                ++OrdinalSize;
            }
            else if (previous == Operation.Codeword)
            {
                _compressed.Add(0, 1);
            }

            //Table 5/V.44 − Code prefix usage 
            _compressed.Add(0, 1);
            _compressed.Add(ch, OrdinalSize);
        }

        /// <summary>
        /// Writes a string representation to the compressed output based on the provided node tree, code word, and
        /// length.
        /// </summary>
        /// <remarks>The method writes a compressed representation of the input data to an internal
        /// buffer.  If the adjusted length is greater than 0, additional metadata is added to represent the string's
        /// extension length.  If the adjusted length is 0, a specific marker is added to indicate the end of the
        /// string.</remarks>
        /// <param name="codeWord">The code word to be written to the compressed output. This is a 16-bit unsigned integer.</param>
        /// <param name="len">The length of the string to be written, adjusted internally by the method. 
        /// Must be greater than or equal to 0.</param>
        private void WriteString(UInt16 codeWord, int len)
        {
            _compressed.Add(1, 1);
            int bits = (int)Math.Floor(Math.Log2(codeWord)) + 1;
            while (bits > CodewordSize)
            {
                //Increase codeword size.
                _compressed.Add(2, CodewordSize);
                ++CodewordSize;
                _compressed.Add(1, 1);
            }

            _compressed.Add(codeWord, CodewordSize);
            if (len > 0)
            {
                //The string-extension procedure is initialized.
                _compressed.Add(2, 2);
                AddStringExtensionLength(_compressed, len);
            }
        }

        /// <summary>Publishes a detached trace snapshot only when a callback is registered.</summary>
        /// <param name="args">Trace context containing the current operation data.</param>
        /// <param name="code">Operation identifier to report.</param>
        /// <param name="data">Token, diagnostic node or message associated with the trace event.</param>
        /// <param name="index">Input or compressed-bit position associated with the event.</param>
        private void NotifyTrace(GXV44TraceArgs args, Operation code, object? data, int index)
        {
            var trace = OnTrace;
            if (trace == null)
            {
                return;
            }

            var snapshot = new GXV44TraceArgs
            {
                Compress = true,
                Operation = code,
                Index = index,
                Value = data,
                Uncompressed = args.Uncompressed?.ToArray(),
                Compressed = _compressed.ToBitString(),
                NodeTree = NodeTree
            };
            trace(snapshot);
            if (snapshot.Stop)
            {
                throw new OperationCanceledException(Resources.Get("CompressionStoppedTrace"));
            }
        }

        /// <summary>Publishes a detached trace snapshot only when a callback is registered.</summary>
        /// <param name="args">Trace context containing the current operation data.</param>
        /// <param name="code">Operation identifier to report.</param>
        /// <param name="node">Dictionary segment to compare or report.</param>
        /// <param name="index">Input or compressed-bit position associated with the event.</param>
        private void NotifyTrace(GXV44TraceArgs args, Operation code, in GXV44EncoderEntry node, int index)
        {
            if (OnTrace != null)
            {
                NotifyTrace(args, code, node.Snapshot(), index);
            }
        }

        /// <summary>Publishes a detached trace snapshot only when a callback is registered.</summary>
        /// <param name="args">Trace context containing the current operation data.</param>
        /// <param name="code">Operation identifier to report.</param>
        /// <param name="codeword">Dictionary codeword identifying a string.</param>
        /// <param name="index">Input or compressed-bit position associated with the event.</param>
        private void NotifyTrace(GXV44TraceArgs args, Operation code, ushort codeword, int index)
        {
            if (OnTrace != null)
            {
                NotifyTrace(args, code, (object)codeword, index);
            }
        }

        /// <summary>Advances the block-local history cursor without copying input bytes.</summary>
        /// <param name="state">Block-local history cursor and input bounds.</param>
        /// <param name="count">Number of consumed input bytes to add to the history cursor.</param>
        private void AdvanceHistory(ref EncodingState state, uint count = 1)
        {
            uint remaining = (uint)_options.HistoryLimit - (state.History - (uint)state.HistoryStart);
            if (count > remaining)
            {
                throw new InvalidOperationException(Resources.Get("EncoderHistoryOverflow"));
            }

            state.History += count;
        }

        /// <summary>Clears encoder dictionaries and restores configured ordinal and codeword widths.</summary>
        public void Reset()
        {
            Array.Clear(_rootArray);
            _nodes.Clear();
            OrdinalSize = (byte)_options.DefaultOrdinalSize;
            CodewordSize = (byte)_options.DefaultCodewordSize;
        }

        /// <summary>Writes REINIT, clears dictionaries and establishes the next local history window.</summary>
        /// <param name="inputIndex">Absolute index of the next unprocessed input byte.</param>
        /// <param name="args">Trace context containing the current operation data.</param>
        /// <param name="state">Block-local history cursor and input bounds.</param>
        private void Reinitialize(int inputIndex, GXV44TraceArgs args, ref EncodingState state)
        {
            _compressed.Add(1, 1);
            _compressed.Add(3, CodewordSize);
            Reset();
            state.History = (uint)inputIndex;
            state.HistoryStart = inputIndex;
            state.InputEnd = inputIndex + Math.Min(_options.HistoryLimit, state.InputEnd - inputIndex);
            NotifyTrace(args, Operation.ReInit, null, inputIndex);
        }

        /// <summary>Links a root byte to its first dictionary segment when no link exists.</summary>
        /// <param name="root">First byte shared by strings under this dictionary root.</param>
        /// <param name="codeWord">Dictionary codeword to emit or link.</param>
        private void AddRootChild(byte root, UInt16 codeWord)
        {
            if (_rootArray[root] != 0)
            {
                return;
            }

            _rootArray[root] = codeWord;
        }

        /// <summary>Compares one complete dictionary segment against input within current history bounds.</summary>
        /// <param name="node">Dictionary segment to compare or report.</param>
        /// <param name="input">Source bytes or readable input stream.</param>
        /// <param name="inputStart">First input index to compare against the segment.</param>
        /// <param name="state">Block-local history cursor and input bounds.</param>
        /// <returns>True if the complete segment matches within the input and history bounds.</returns>
        private static bool SegmentMatches(in GXV44EncoderEntry node, byte[] input, int inputStart, in EncodingState state)
        {
            if (inputStart + node.Length > state.InputEnd)
            {
                return false;
            }

            if (node.Position + node.Length > state.History)
            {
                return false;
            }

            for (int i = 0; i < node.Length; i++)
            {
                if (input[inputStart + i] != input[node.Position + i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Traverses dictionary child and sibling links to find the longest complete string match.</summary>
        /// <param name="codeWord">Dictionary codeword to emit or link.</param>
        /// <param name="input">Source bytes or readable input stream.</param>
        /// <param name="nextInputIndex">Input index immediately after the implicit root character.</param>
        /// <param name="state">Block-local history cursor and input bounds.</param>
        /// <returns>The matched node index and full input length, or a negative index when no dictionary string matches.</returns>
        private GXCompressionInfo FindLongestMatch(UInt16 codeWord, byte[] input, int nextInputIndex, in EncodingState state)
        {
            int bestIndex = -1;
            int matchedTail = 0;
            int accumulatedTail = 0;
            int levelIndex = codeWord - 4;
            int cursor = nextInputIndex;
            while (levelIndex >= 0)
            {
                int matchingIndex = -1;
                while (true)
                {
                    ref var level = ref _nodes[levelIndex];
                    if (1 + accumulatedTail + level.Length <= (int)_options.MaximumStringLength && SegmentMatches(level, input, cursor, in state))
                    {
                        matchingIndex = levelIndex;
                        break;
                    }
                    if (level.SideIndex == 0)
                    {
                        break;
                    }
                    levelIndex = level.SideIndex - 4;
                }

                if (matchingIndex < 0)
                {
                    break;
                }

                ref var matched = ref _nodes[matchingIndex];
                bestIndex = matchingIndex;
                accumulatedTail += matched.Length;
                matchedTail = accumulatedTail;
                cursor += matched.Length;
                levelIndex = matched.DownIndex == 0 ? -1 : matched.DownIndex - 4;
            }

            return new GXCompressionInfo
            {
                NodeIndex = bestIndex,
                TotalStringLength = bestIndex < 0 ? 0 : 1 + matchedTail
            };
        }

        /// <summary>
        /// Adds a sibling node to the node tree under the specified root node.
        /// </summary>
        /// <param name="root">The index of the root node under which the sibling node is added.</param>
        /// <param name="node">The node to add as a sibling.</param>
        private void AddSiblingUnderRoot(byte root, GXV44EncoderEntry node)
        {
            var head = _rootArray[root];
            if (head == 0)
            {
                AddRootChild(root, head);
                return;
            }

            ref GXV44EncoderEntry current = ref _nodes[head - 4];
            while (current.SideIndex != 0)
            {
                current = ref _nodes[current.SideIndex - 4];
            }

            current.SideIndex = node.Codeword;
        }

        /// <summary>
        /// Compresses the specified byte array using a V.44 compression algorithm.
        /// </summary>
        /// <remarks>This method implements a V.44 compression algorithm to reduce the size of the input
        /// byte array.  The compression process involves building a tree structure and encoding repeated patterns in the
        /// input data.  The method ensures that the compressed data is properly terminated with flush bits to indicate
        /// the end of the stream.  Note that the compression algorithm is optimized for specific use cases and may not
        /// achieve optimal compression ratios for all types of input data. The caller should handle the returned
        /// compressed data appropriately.</remarks>
        /// <param name="uncompressed">String to be compressed. Cannot be null.</param>
        /// <returns>A byte array containing the compressed data. Empty input produces a FLUSH-only stream.</returns>
        public byte[] Compress(string uncompressed)
        {
            ArgumentNullException.ThrowIfNull(uncompressed);
            return Compress(ASCIIEncoding.ASCII.GetBytes(uncompressed));
        }

        /// <summary>
        /// Adds a child segment to the specified parent node in the node tree.
        /// </summary>
        /// <param name="parent">The parent node to which the child segment is added.</param>
        /// <param name="node">The child node to add.</param>
        private void AddChildSegment(ref GXV44EncoderEntry parent, GXV44EncoderEntry node)
        {
            if (parent.DownIndex == 0)
            {
                parent.DownIndex = node.Codeword;
                return;
            }

            ref GXV44EncoderEntry current = ref _nodes[parent.DownIndex - 4];
            while (current.SideIndex != 0)
            {
                current = ref _nodes[current.SideIndex - 4];
            }

            current.SideIndex = node.Codeword;
        }

        /// <summary>
        /// Compresses the specified byte array using a V.44 compression algorithm.
        /// </summary>
        /// <remarks>This method implements a V.44 compression algorithm to reduce the size of the input
        /// byte array.  The compression process involves building a tree structure and encoding repeated patterns in the
        /// input data.  The method ensures that the compressed data is properly terminated with flush bits to indicate
        /// the end of the stream.  Note that the compression algorithm is optimized for specific use cases and may not
        /// achieve optimal compression ratios for all types of input data. The caller should handle the returned
        /// compressed data appropriately.</remarks>
        /// <returns>A byte array containing the compressed data. Empty input produces a FLUSH-only stream.</returns>
        /// <param name="input">Source bytes or readable input stream.</param>
        public byte[] Compress(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            CompressBlock(input, input.Length, true);
            return _compressed.Array();
        }

        /// <summary>Compresses a stream using bounded input blocks; leaves both streams open.</summary>
        /// <param name="input">Source bytes or readable input stream.</param>
        /// <param name="output">Writable destination that remains open after processing.</param>
        /// <exception cref="ArgumentNullException">An input or output stream is null.</exception>
        /// <exception cref="ArgumentException">The input is unreadable, the output is unwritable, or both streams are the same instance.</exception>
        /// <exception cref="IOException">Reading or writing a stream fails.</exception>
        public void Compress(Stream input, Stream output)
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

            byte[] block = new byte[Math.Min(_options.HistoryLimit, 65536)];
            var writer = new GXV44BitWriter(output);
            int pending = -1;
            do
            {
                int count = 0;
                if (pending >= 0)
                {
                    block[count++] = (byte)pending;
                }

                while (count < block.Length)
                {
                    int read = input.Read(block, count, block.Length - count);
                    if (read == 0)
                    {
                        break;
                    }

                    count += read;
                }

                pending = input.ReadByte();
                CompressBlock(block, count, pending < 0);
                if (pending >= 0)
                {
                    _compressed.Add(1, 1);
                    _compressed.Add(3, CodewordSize);
                }

                writer.Write(_compressed);
            }
            while (pending >= 0);
            writer.Align();
        }

        /// <summary>Encodes one valid input block with local history counters and optional terminal FLUSH.</summary>
        /// <param name="input">Source bytes or readable input stream.</param>
        /// <param name="inputLength">Number of valid bytes in the supplied input array.</param>
        /// <param name="flush">True to append terminal FLUSH; false when another input block follows.</param>
        private void CompressBlock(byte[] input, int inputLength, bool flush)
        {
            ArgumentNullException.ThrowIfNull(input);
            _compressed.Size = 0;
            _compressed.Position = 0;
            GXV44TraceArgs args = new()
            {
                Compress = true,
                Uncompressed = OnTrace == null ? null : input.AsSpan(0, inputLength).ToArray(),
                NodeTree = null
            };
            //Initialize values.
            OrdinalSize = (byte)_options.DefaultOrdinalSize;
            CodewordSize = (byte)_options.DefaultCodewordSize;
            Reset();
            _nodes.Clear();
            var state = new EncodingState
            {
                InputEnd = Math.Min(inputLength, _options.HistoryLimit)
            };
            NotifyTrace(args, Operation.Start, null, 0);
            GXV44EncoderEntry node;
            //Is string extension used for ordinal or codeword.
            Operation previous = Operation.Ordinal;
            int i = 0;
            while (i < inputLength)
            {
                if (i == state.InputEnd || 4 + _nodes.Count >= _options.MaxCodewords)
                {
                    state.InputEnd = inputLength;
                    Reinitialize(i, args, ref state);
                    previous = Operation.None;
                }

                byte ch = input[i];
                AdvanceHistory(ref state);
                var rootChild = _rootArray[ch];
                if (rootChild == 0)
                {
                    WriteOrdinal(ch, previous);
                    previous = Operation.Ordinal;
                    node = new GXV44EncoderEntry((UInt16)(4 + _nodes.Count), (1 + i), 1);
                    node.SetDiagnosticData(input, i, 1, _options.Hex);
                    _nodes.Add(node);
                    if (i + 1 < inputLength)
                    {
                        // Capacity is checked at the next token boundary.
                        AddRootChild(ch, node.Codeword);
                    }

                    ++i;
                    NotifyTrace(args, Operation.Ordinal, node, i);
                    continue;
                }

                var match = FindLongestMatch(rootChild, input, i + 1, in state);
                if (match.NodeIndex < 0)
                {
                    WriteOrdinal(ch, previous);
                    previous = Operation.Ordinal;
                    node = new GXV44EncoderEntry((UInt16)(4 + _nodes.Count), (1 + i), 1);
                    node.SetDiagnosticData(input, i, 1, _options.Hex);
                    _nodes.Add(node);
                    NotifyTrace(args, Operation.Ordinal, node, i);
                    if (i + 1 < inputLength)
                    {
                        // Capacity is checked at the next token boundary.
                        AddSiblingUnderRoot(ch, node);
                    }

                    i += 1;
                    continue;
                }

                ref var matchedNode = ref _nodes[match.NodeIndex];
                AdvanceHistory(ref state, (uint)(match.TotalStringLength - 1));
                int consumed = match.TotalStringLength;
                // String-extension: compare subsequent input chars with chars immediately following
                // the last completely matched string-segment in history.
                int extensionLength = 0;
                int extensionStartInput = i + consumed;
                int extensionSourceHistory = matchedNode.Position + matchedNode.Length;
                //string-extension length is MaximumStringLength - 2 (N7T − 2).
                int maxExtension = (int)_options.MaximumStringLength - match.TotalStringLength;
                while (extensionLength < maxExtension && extensionStartInput + extensionLength < state.InputEnd && extensionSourceHistory + extensionLength < state.History)
                {
                    byte incoming = input[extensionStartInput + extensionLength];
                    byte historyByte = input[extensionSourceHistory + extensionLength];
                    if (incoming != historyByte)
                    {
                        break;
                    }

                    AdvanceHistory(ref state);
                    extensionLength++;
                }

                if (extensionLength > 0)
                {
                    node = new GXV44EncoderEntry((UInt16)(4 + _nodes.Count), (i + match.TotalStringLength), (UInt16)extensionLength)
                    {
                        Type = Operation.StringExtension
                    };
                    node.SetDiagnosticData(input, i, match.TotalStringLength + extensionLength, _options.Hex);
                    AddChildSegment(ref matchedNode, node);
                    _nodes.Add(node);
                    WriteString(matchedNode.Codeword, extensionLength);
                    NotifyTrace(args, Operation.Processing, matchedNode.Codeword, 0);
                    if (extensionLength > 0)
                    {
                        previous = Operation.StringExtension;
                    }
                    else
                    {
                        previous = Operation.Codeword;
                    }

                    consumed += extensionLength;
                }
                else
                {
                    UInt16 codeWord = matchedNode.Codeword;
                    WriteString(codeWord, 0);
                    node = new GXV44EncoderEntry((UInt16)(4 + _nodes.Count), (i + match.TotalStringLength), 1)
                    {
                        Type = Operation.Codeword
                    };
                    if (i + 2 == inputLength)
                    {
                        //If data is ending to codeword.
                        node.SetDiagnosticData(input, i, 2, _options.Hex);
                    }
                    else
                    {
                        node.SetDiagnosticData(input, i, match.TotalStringLength, _options.Hex);
                    }

                    if (match.TotalStringLength < (int)_options.MaximumStringLength)
                    {
                        AddChildSegment(ref matchedNode, node);
                        _nodes.Add(node);
                    }

                    NotifyTrace(args, Operation.Codeword, node, 0);
                    NotifyTrace(args, Operation.Processing, codeWord, 0);
                    previous = Operation.Codeword;
                }

                i += consumed;
            }

            if (flush)
            {
                _compressed.Add(1, 1);
                _compressed.Add(1, CodewordSize);
            }

            NotifyTrace(args, Operation.End, null, 0);
        }
    }
}
