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
using System.Text;

namespace Gurux.Compression
{
    /// <summary>
    /// Detached diagnostic information passed to a V.44 trace callback.
    /// </summary>
    public class GXV44TraceArgs
    {
        /// <summary>
        /// True for encoder events; false for decoder events.
        /// </summary>
        public bool Compress;

        /// <summary>
        /// Identifies the operation represented by this event.
        /// </summary>
        public Operation Operation;

        /// <summary>
        /// Value associated with the operation.
        /// </summary>
        public object? Value;

        /// <summary>
        /// Detached uncompressed bytes associated with the event, or null when unavailable.
        /// </summary>
        public byte[]? Uncompressed;

        /// <summary>
        /// Compressed bits in transmission order, containing only zero and one characters; null when unavailable.
        /// </summary>
        public string? Compressed;

        /// <summary>
        /// Input-byte or compressed-bit position associated with the event.
        /// </summary>
        public int Index;

        /// <summary>
        /// Detached encoder dictionary snapshot, or null for decoder events.
        /// </summary>
        public IReadOnlyList<GXv44Node>? NodeTree;

        /// <summary>
        /// Detached decoder dictionary snapshot, or null for encoder events.
        /// </summary>
        public IReadOnlyList<GXv44DecoderEntry>? StringCollection;

        /// <summary>
        /// Set to true inside the callback to cancel processing with OperationCanceledException.
        /// </summary>
        public bool Stop;

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new();
            if (Operation == Operation.Start || Operation == Operation.End)
            {
                if (Compress)
                {
                    sb.Append("Compress ");
                }
                else
                {
                    sb.Append("Decompress ");
                }
            }

            switch (Operation)
            {
                case Operation.Start:
                    sb.Append("Start");
                    break;
                case Operation.End:
                    sb.Append("End");
                    break;
                case Operation.Ordinal:
                case Operation.Codeword:
                case Operation.StringExtension:
                    sb.Append(Value);
                    break;
                case Operation.StepUpOrdinal:
                    sb.Append("StepUp Ordinal");
                    break;
                case Operation.StepUpCodeword:
                    sb.Append("StepUp Codeword");
                    break;
                case Operation.Flush:
                    sb.Append("Flush");
                    break;
                case Operation.ReInit:
                    sb.Append("ReInit");
                    break;
                case Operation.Error:
                    sb.Append("Error");
                    break;
                case Operation.Processing:
                    sb.Append("CW(");
                    sb.Append(Value);
                    sb.Append(')');
                    break;
            }

            return sb.ToString();
        }
    }
}
