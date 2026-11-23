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
using System.Text;
using Gurux.Compression.Properties;

namespace Gurux.Compression
{
    /// <summary>A least-significant-bit-first bit buffer.</summary>
    internal class GXBitArray
    {
        /// <summary>Number of valid bits currently stored.</summary>
        private int _size;

        /// <summary>Read cursor measured from the first bit.</summary>
        private int _position;

        /// <summary>Byte storage containing least-significant-bit-first fields.</summary>
        private byte[] _data = System.Array.Empty<byte>();

        /// <summary>Gets or changes the bit capacity, truncating valid data when reduced.</summary>
        public int Capacity
        {
            get => checked(_data.Length * 8);
            set
            {
                if (value < 0 || value > int.MaxValue - 7)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                int bytes = (value + 7) / 8;
                if (value < _size)
                {
                    Size = value;
                }

                System.Array.Resize(ref _data, bytes);
            }
        }

        /// <summary>Gets or changes the valid bit count; reducing it clears removed bits and clamps the read cursor.</summary>
        public int Size
        {
            get => _size;
            set
            {
                if (value < 0 || value > Capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value < _size)
                {
                    int start = (value + 7) / 8;
                    if (value % 8 != 0)
                    {
                        _data[value / 8] &= (byte)((1 << (value % 8)) - 1);
                    }

                    System.Array.Clear(_data, start, _data.Length - start);
                }

                _size = value;
                _position = Math.Min(_position, value);
            }
        }

        /// <summary>Gets or changes the read cursor within the valid bit range.</summary>
        public int Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = value;
            }
        }

        /// <summary>Gets the number of valid bits remaining after the read cursor.</summary>
        public int Available => Size - Position;

        /// <summary>Returns a detached array containing only the used bytes, with zero padding.</summary>
        /// <returns>A detached byte array containing used bytes and zero padding in its final byte.</returns>
        public byte[] Array() => _data.AsSpan(0, (Size + 7) / 8).ToArray();

        /// <summary>Exposes the used bytes internally without allocating a detached array.</summary>
        internal ReadOnlySpan<byte> WrittenSpan => _data.AsSpan(0, (Size + 7) / 8);

        /// <summary>Reads a bit without advancing the read cursor.</summary>
        /// <param name="index">Zero-based starting bit index.</param>
        /// <returns>The selected bit or least-significant-bit-first field value.</returns>
        public bool Get(int index) => Get(index, 1) != 0;

        /// <summary>Reads 1–32 bits, least significant bit first.</summary>
        /// <param name="index">Zero-based starting bit index.</param>
        /// <param name="count">Number of low-order bits to read or append, from one through 32.</param>
        /// <returns>The selected bit or least-significant-bit-first field value.</returns>
        public int Get(int index, int count)
        {
            if (count < 1 || count > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (index < 0 || index > Size - count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int offset = index & 7;
            int bytes = (offset + count + 7) >> 3;
            ulong accumulator = 0;
            for (int i = 0; i < bytes; ++i)
            {
                accumulator |= (ulong)_data[(index >> 3) + i] << (8 * i);
            }

            return unchecked((int)((accumulator >> offset) & (uint.MaxValue >> (32 - count))));
        }

        /// <summary>Grows byte storage geometrically to fit an appended field.</summary>
        /// <param name="bits">Required bit capacity.</param>
        private void EnsureCapacity(int bits)
        {
            if (bits <= Capacity)
            {
                return;
            }

            long grown = Math.Max(bits, Math.Max(64L, (long)Capacity * 2));
            Capacity = (int)Math.Min(grown, int.MaxValue - 7);
        }

        /// <summary>Appends all bytes while preserving an existing partial-byte alignment.</summary>
        /// <param name="value">Bytes or a bit value to append in transmission order.</param>
        public void Add(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            int end = checked(Size + checked(value.Length * 8));
            EnsureCapacity(end);
            if (Size % 8 == 0)
            {
                value.CopyTo(_data, Size / 8);
                _size = end;
            }
            else
            {
                foreach (byte b in value)
                {
                    Add(b, 8);
                }
            }
        }

        /// <summary>Appends zero and one characters, ignoring whitespace.</summary>
        /// <param name="bitString">Binary digits to append; whitespace is ignored.</param>
        public void Add(string bitString)
        {
            ArgumentNullException.ThrowIfNull(bitString);
            foreach (char bit in bitString)
            {
                if (bit == '0' || bit == '1')
                {
                    Add(bit == '1');
                }
                else if (!char.IsWhiteSpace(bit))
                {
                    throw new ArgumentException(Resources.Get("BitBufferExpectedDigits"), nameof(bitString));
                }
            }
        }

        /// <summary>Appends the low-order bits of the supplied value.</summary>
        /// <param name="value">Bytes or a bit value to append in transmission order.</param>
        public void Add(bool value) => Add(value ? 1 : 0, 1);

        /// <summary>Appends the low-order bits of the supplied value.</summary>
        /// <param name="value">Bytes or a bit value to append in transmission order.</param>
        public void Add(int value) => Add(value, 1);

        /// <summary>Appends the low-order bits of the supplied value.</summary>
        /// <param name="value">Bytes or a bit value to append in transmission order.</param>
        /// <param name="count">Number of low-order bits to read or append, from one through 32.</param>
        public void Add(int value, int count)
        {
            if (count < 1 || count > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EnsureCapacity(checked(Size + count));
            int offset = _size & 7;
            int first = _size >> 3;
            ulong mask = uint.MaxValue >> (32 - count);
            ulong accumulator = ((ulong)(uint)value & mask) << offset;
            accumulator |= (uint)(_data[first] & ((1 << offset) - 1));
            int bytes = (offset + count + 7) >> 3;
            for (int i = 0; i < bytes; ++i)
            {
                _data[first + i] = (byte)(accumulator >> (8 * i));
            }

            _size += count;
        }

        /// <summary>Formats exactly the valid bits in transmission order without byte separators or padding.</summary>
        /// <returns>A string containing one zero or one character per valid bit.</returns>
        internal string ToBitString()
        {
            return string.Create(Size, this, static (characters, bits) =>
            {
                for (int i = 0; i < characters.Length; ++i)
                {
                    characters[i] = bits.Get(i) ? '1' : '0';
                }
            });
        }

        /// <summary>Formats valid bits with spaces at byte boundaries.</summary>
        /// <returns>The formatted representation of the valid data.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Size; ++i)
            {
                if (i != 0 && i % 8 == 0)
                {
                    sb.Append(' ');
                }

                sb.Append(Get(i) ? '1' : '0');
            }

            return sb.ToString();
        }

        /// <summary>Formats valid bits with spaces at byte boundaries.</summary>
        /// <param name="hex">True to format hexadecimal bytes; false to format bits with byte separators.</param>
        /// <returns>The formatted representation of the valid data.</returns>
        public string ToString(bool hex) => hex ? Convert.ToHexString(_data, 0, (Size + 7) / 8) : ToString();
    }
}
