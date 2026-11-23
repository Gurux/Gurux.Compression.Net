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

namespace Gurux.Compression.Internal;

/// <summary>Formats dictionary diagnostics only when explicitly inspected.</summary>
/// <param name="data">Source bytes retained for diagnostic formatting.</param>
/// <param name="index">Zero based start index in the source or dictionary.</param>
/// <param name="count">Number of source bytes retained for formatting.</param>
/// <param name="hex">Whether to force hexadecimal diagnostic formatting.</param>
internal struct GXV44DiagnosticValue(ReadOnlyMemory<byte> data, int index, int count, bool hex)
{
    /// <summary>Input data retained until its diagnostic representation is first requested.</summary>
    private ReadOnlyMemory<byte>? _data = data;

    /// <summary>Cached diagnostic representation, or null before formatting.</summary>
    private string? _value;

    /// <summary>Gets or replaces the cached ASCII or hexadecimal diagnostic representation.</summary>
    public string? Value
    {
        get
        {
            if (_value != null || _data == null)
            {
                return _value;
            }

            var span = _data.Value.Span.Slice(index, count);
            bool ascii = !hex;
            foreach (byte b in span)
            {
                if (b > 127 || b is 0 or 10 or 13)
                {
                    ascii = false;
                    break;
                }
            }
            _value = ascii ? Encoding.UTF8.GetString(span) : "0x" + Convert.ToHexString(span);
            _data = null;
            return _value;
        }

        set
        {
            _value = value;
            _data = null;
        }
    }
}
