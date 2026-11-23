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

/// <summary>Fixed-capacity, reusable dictionary storage. Returned references remain stable until reset.</summary>
/// <typeparam name = "T">Value type stored in the dictionary.</typeparam>
/// <param name="capacity">Maximum number of dictionary entries.</param>
internal sealed class GXV44Table<T>(int capacity)
    where T : struct
{
    /// <summary>Reusable dictionary entry storage allocated at construction.</summary>
    private readonly T[] _items = new T[capacity];

    /// <summary>Gets the number of entries currently stored.</summary>
    public int Count { get; private set; }

    /// <summary>Gets a reference to an existing entry without copying it.</summary>
    /// <param name="index">Zero based index of an existing entry.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index does not identify a stored entry.</exception>
    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ref _items[index];
        }
    }

    /// <summary>Appends an entry to the fixed capacity dictionary.</summary>
    /// <param name="item">Entry to append to the dictionary.</param>
    /// <exception cref="InvalidOperationException">The dictionary capacity is exhausted.</exception>
    public void Add(in T item)
    {
        if (Count == _items.Length)
        {
            throw new InvalidOperationException(Resources.Get("InternalDictionaryCapacityExceeded"));
        }

        _items[Count++] = item;
    }

    /// <summary>Removes all entries and releases references retained by their diagnostic data.</summary>
    public void Clear()
    {
        // Release diagnostic input references without reallocating the storage.
        Array.Clear(_items, 0, Count);
        Count = 0;
    }
}
