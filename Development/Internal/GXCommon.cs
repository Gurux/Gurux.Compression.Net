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
using System.Diagnostics;

namespace Gurux.Compression.Internal
{
    /// <summary>Provides byte comparison and hexadecimal conversion helpers.</summary>
    public class GXCommon
    {
        /// <summary>Determines whether two byte arrays contain identical bytes.</summary>
        /// <param name="bytes">Byte array to inspect or format.</param>
        /// <param name="arr">Byte array to compare against.</param>
        /// <returns>True if both arrays contain the same bytes; otherwise false.</returns>
        internal static bool Compare(byte[] bytes, byte[] arr)
        {
            return bytes.AsSpan().SequenceEqual(arr);
        }

        /// <summary>Converts hexadecimal text, optionally containing spaces, to bytes.</summary>
        /// <param name="value">Hexadecimal text to decode.</param>
        /// <returns>The decoded bytes.</returns>
        public static byte[] HexToBytes(string value)
        {
            return Convert.FromHexString(value.Replace(" ", ""));
        }

        /// <summary>Formats bytes as uppercase hexadecimal text with optional spaces.</summary>
        /// <param name="bytes">Byte array to inspect or format.</param>
        /// <param name="addSpace">Whether to separate hexadecimal byte values with spaces.</param>
        /// <returns>The formatted hexadecimal representation.</returns>
        [DebuggerStepThrough]
        public static string ToHex(byte[] bytes, bool addSpace = true)
        {
            return ToHex(bytes, addSpace, 0, bytes == null ? 0 : bytes.Length);
        }

        /// <summary>
        /// Convert byte array to hex string.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <param name="addSpace">Is space added between bytes.</param>
        /// <param name="index">Zero based start index in the array.</param>
        /// <param name="count">Number of bytes to format.</param>
        /// <returns>Byte array as hex string.</returns>
        [DebuggerStepThrough]
        public static string ToHex(byte[] bytes, bool addSpace, int index, int count)
        {
            if (bytes == null || bytes.Length == 0 || count == 0)
            {
                return string.Empty;
            }

            char[] str = new char[count * 3];
            int tmp;
            int len = 0;
            for (int pos = 0; pos != count; ++pos)
            {
                tmp = (bytes[index + pos] >> 4);
                str[len] = (char)(tmp > 9 ? tmp + 0x37 : tmp + 0x30);
                ++len;
                tmp = (bytes[index + pos] & 0x0F);
                str[len] = (char)(tmp > 9 ? tmp + 0x37 : tmp + 0x30);
                ++len;
                if (addSpace)
                {
                    str[len] = ' ';
                    ++len;
                }
            }

            if (addSpace)
            {
                --len;
            }

            return new string (str, 0, len);
        }
    }
}
