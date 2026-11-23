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
    /// V.44 encoder node.
    /// </summary>
    public class GXv44Node
    {
        /// <summary>
        /// Creates an empty diagnostic node with no dictionary links.
        /// </summary>
        public GXv44Node()
        {
        }

        /// <summary>
        /// Creates a diagnostic segment with its codeword and history range.
        /// </summary>
        /// <param name="codeword">Dictionary codeword identifying a string.</param>
        /// <param name="position">Zero-based history offset at which the dictionary segment starts.</param>
        /// <param name="length">Number of bytes in the dictionary segment.</param>
        public GXv44Node(UInt16 codeword, int position, UInt16 length)
        {
            Codeword = codeword;
            Position = position;
            Length = length;
        }

        /// <summary>
        /// Gets or sets the human-readable string or hexadecimal value captured for diagnostics.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Represents the codeword value associated with this instance.
        /// </summary>
        public UInt16 Codeword { get; set; }

        /// <summary>
        /// Gets or sets the operation that created this diagnostic node.
        /// </summary>
        public Operation Type { get; set; }

        /// <summary>
        /// Gets or sets the zero-based history offset of the first byte in this segment.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes in this dictionary segment.
        /// </summary>
        public UInt16 Length { get; set; }

        /// <summary>
        /// Down index points to a node that represents a string-segment that follows this string-segment.
        /// </summary>
        public UInt16 DownIndex { get; set; }

        /// <summary>
        /// Side index points to a node that represents a string-segment at the same level as this string-segment.
        /// </summary>
        public UInt16 SideIndex { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Length == 0)
            {
                sb.Append("'");
                sb.Append(Value);
                sb.Append("' ");
            }
            else
            {
                sb.Append("Codeword:");
                sb.Append(Codeword);
                sb.Append(" ");
                sb.Append(Value);
                sb.Append(" Position:");
                sb.Append(Position);
                sb.Append(" Length: ");
                sb.Append(Length);
                if (DownIndex != 0)
                {
                    sb.Append(" DownIndex: ");
                    sb.Append(DownIndex);
                }

                if (SideIndex != 0)
                {
                    sb.Append(" SideIndex: ");
                    sb.Append(SideIndex);
                }
            }

            return sb.ToString();
        }
    }
}
