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
namespace Gurux.Compression.Internal;

/// <summary>Joins bit buffers without inserting padding between dictionary epochs.</summary>
/// <param name="output">Stream receiving the encoded bytes.</param>
internal sealed class GXV44BitWriter(Stream output)
{
    /// <summary>Bits awaiting a complete output byte.</summary>
    private int _pending;

    /// <summary>Number of pending bits in the output accumulator.</summary>
    private int _count;

    /// <summary>Appends a bit buffer without padding its final partial byte.</summary>
    /// <param name="bits">Bit buffer to consume.</param>
    public void Write(GXBitArray bits)
    {
        ReadOnlySpan<byte> bytes = bits.WrittenSpan;
        for (int i = 0; i < bytes.Length; ++i)
        {
            int width = Math.Min(8, bits.Size - i * 8);
            _pending |= (bytes[i] & ((1 << width) - 1)) << _count;
            _count += width;
            if (_count >= 8)
            {
                output.WriteByte((byte)_pending);
                _pending >>= 8;
                _count -= 8;
            }
        }
    }

    /// <summary>Writes any pending bits with zero padding to complete the output byte.</summary>
    public void Align()
    {
        if (_count == 0)
        {
            return;
        }

        output.WriteByte((byte)_pending);
        _pending = _count = 0;
    }
}
