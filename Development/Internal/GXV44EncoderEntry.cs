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

namespace Gurux.Compression.Internal;

/// <summary>Stores encoder dictionary links, history coordinates, and diagnostic data.</summary>
/// <param name="codeword">Codeword identifying the dictionary entry.</param>
/// <param name="position">Position of the represented string in history.</param>
/// <param name="length">Number of bytes represented by the entry.</param>
internal struct GXV44EncoderEntry(ushort codeword, int position, ushort length)
{
    /// <summary>Codeword identifying this encoder dictionary entry.</summary>
    public ushort Codeword = codeword;

    /// <summary>Position of the represented string in history.</summary>
    public int Position = position;

    /// <summary>Number of history bytes represented by this entry.</summary>
    public ushort Length = length;

    /// <summary>Index of the first child dictionary entry.</summary>
    public ushort DownIndex;

    /// <summary>Index of the next sibling dictionary entry.</summary>
    public ushort SideIndex;

    /// <summary>Operation associated with this dictionary entry.</summary>
    public Operation Type;

    /// <summary>Lazily formatted diagnostic representation of the entry.</summary>
    private GXV44DiagnosticValue _diagnostic;

    /// <summary>Retains the specified history slice for lazy diagnostic formatting.</summary>
    /// <param name="data">Source bytes retained for diagnostic formatting.</param>
    /// <param name="index">Zero based start index in the source or dictionary.</param>
    /// <param name="count">Number of source bytes retained for formatting.</param>
    /// <param name="hex">Whether to force hexadecimal diagnostic formatting.</param>
    public void SetDiagnosticData(byte[] data, int index, int count, bool hex) => _diagnostic = new GXV44DiagnosticValue(data, index, count, hex);

    /// <summary>Releases retained diagnostic data and its cached representation.</summary>
    public void ClearDiagnostics() => _diagnostic = default;

    /// <summary>Creates a public diagnostic copy of this dictionary entry.</summary>
    /// <returns>A diagnostic copy independent of dictionary storage.</returns>
    public GXv44Node Snapshot() => new(Codeword, Position, Length)
    {
        Value = _diagnostic.Value ?? string.Empty,
        Type = Type,
        DownIndex = DownIndex,
        SideIndex = SideIndex
    };
}
