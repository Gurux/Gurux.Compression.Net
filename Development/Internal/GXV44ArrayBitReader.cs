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

/// <summary>Adapts an in memory bit array to the V.44 bit reader contract.</summary>
/// <param name="bits">Bit buffer to consume.</param>
/// <param name="position">Initial bit position within the buffer.</param>
internal struct GXV44ArrayBitReader(GXBitArray bits, int position) : IGXV44BitReader
{
    /// <summary>Gets the next bit position to read from the bit array.</summary>
    public int Position { get; private set; } = position;

    /// <summary>Reads bits at the current position and advances by the requested count.</summary>
    /// <param name="count">Number of bits to consume.</param>
    /// <returns>The bits read as an unsigned value.</returns>
    public int Read(int count)
    {
        int value = bits.Get(Position, count);
        Position += count;
        return value;
    }
}
