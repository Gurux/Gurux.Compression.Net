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
namespace Gurux.Compression
{
    /// <summary>
    /// GXv44DecoderEntry contains information about codeword, 
    /// position and length of the string of the decoder entry.
    /// </summary>
    public sealed class GXv44DecoderEntry
    {
        /// <summary>
        /// Represents the codeword value associated with this instance.
        /// </summary>
        public UInt16 Codeword { get; init; }

        /// <summary>
        /// Gets the zero-based position of the final byte represented by this dictionary string.
        /// </summary>
        public int Position { get; init; }

        /// <summary>
        /// Gets the number of decoded bytes represented by the dictionary string.
        /// </summary>
        public int Length { get; init; }

        /// <summary>Gets the first history position covered by this string, calculated from its final position and length.</summary>
        public int StartPos => Position - Length + 1;

        /// <summary>
        /// Gets or sets a human-readable or hexadecimal diagnostic value, or null when cleared.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        /// <returns>A string representation of the current object.</returns>
        public override string ToString()
        {
            return $"{{Codeword:{Codeword} Position:{Position} Length:{Length} Value:{Value}}}";
        }

        /// <summary>
        /// Creates an empty diagnostic entry whose init properties can be populated by the caller.
        /// </summary>
        public GXv44DecoderEntry()
        {
        }

        /// <summary>
        /// Creates a diagnostic dictionary entry covering a decoded history range.
        /// </summary>
        /// <param name="codeword">Dictionary codeword identifying the string.</param>
        /// <param name="position">History position associated with the dictionary entry.</param>
        /// <param name="length">Number of bytes represented by the dictionary entry.</param>
        public GXv44DecoderEntry(UInt16 codeword, int position, int length)
        {
            Codeword = codeword;
            Position = position;
            Length = length;
        }
    }
}
