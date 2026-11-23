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
using Gurux.Compression.Properties;

namespace Gurux.Compression.Internal;

/// <summary>Strict LSB-first reader with at most one token of lookahead.</summary>
/// <param name="input">Stream supplying the encoded bytes.</param>
internal sealed class GXV44BitReader(Stream input) : IGXV44BitReader
{
    /// <summary>Buffered bits awaiting consumption, with the next bit in the least significant position.</summary>
    private uint _buffer;

    /// <summary>Number of unread bits currently buffered.</summary>
    private int _available;

    /// <summary>Gets the number of bits consumed from the input stream.</summary>
    public long Position { get; private set; }

    /// <summary>Examines the next bit without advancing the reader.</summary>
    /// <param name="bit">Receives the next bit, or zero if the stream has ended.</param>
    /// <returns>True if a bit is available; otherwise false.</returns>
    public bool TryPeek(out int bit)
    {
        if (_available == 0)
        {
            int next = input.ReadByte();
            if (next < 0)
            {
                bit = 0;
                return false;
            }

            _buffer = (uint)next;
            _available = 8;
        }

        bit = (int)(_buffer & 1);
        return true;
    }

    /// <summary>Consumes the requested number of bits in least significant bit order.</summary>
    /// <param name="count">Number of bits to consume.</param>
    /// <returns>The bits read as an unsigned value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the supported range of one to sixteen bits.</exception>
    /// <exception cref="InvalidDataException">The stream ends before the requested bits are available.</exception>
    public int Read(int count)
    {
        if (count < 1 || count > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        while (_available < count)
        {
            int next = input.ReadByte();
            if (next < 0)
            {
                throw new InvalidDataException(Resources.Format("InternalTruncatedToken", Position + _available));
            }

            _buffer |= (uint)next << _available;
            _available += 8;
        }

        int result = (int)(_buffer & ((1u << count) - 1));
        _buffer >>= count;
        _available -= count;
        Position += count;
        return result;
    }

    /// <summary>Consumes and validates zero padding up to the next byte boundary.</summary>
    /// <exception cref="InvalidDataException">Padding bits are nonzero or truncated.</exception>
    public void Align()
    {
        int padding = (int)((8 - Position % 8) % 8);
        if (padding != 0 && Read(padding) != 0)
        {
            throw new InvalidDataException(Resources.Format("InternalNonzeroFlushPadding", Position));
        }
    }
}
