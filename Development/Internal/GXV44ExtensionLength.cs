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
using Gurux.Compression.Enums;

namespace Gurux.Compression.Internal;

/// <summary>Shared V.44 extension-length decoding for stream and bit-array readers.</summary>
internal static class GXV44ExtensionLength
{
    // The constrained generic call mutates a struct reader without boxing and
    // also accepts the existing stream reader without per-token allocations.

    /// <summary>Decodes an extension length using the negotiated V.44 length limits.</summary>
    /// <typeparam name = "T">Reader implementation used without boxing value types.</typeparam>
    /// <param name="reader">Reader whose position advances as encoded bits are consumed.</param>
    /// <param name="maximum">Negotiated maximum string length.</param>
    /// <returns>The decoded extension length in bytes.</returns>
    /// <exception cref="InvalidDataException">The length option or encoded extension is invalid.</exception>
    public static int Read<T>(ref T reader, MaximumStringLength maximum)
        where T : IGXV44BitReader
    {
        if (reader.Read(1) == 1)
        {
            return 1;
        }

        int shortLength = reader.Read(2);
        if (shortLength != 0)
        {
            return shortLength + 1;
        }

        if (reader.Read(1) == 0)
        {
            return reader.Read(3) + 5;
        }

        int width = maximum switch
        {
            MaximumStringLength.Value46 => 5,
            MaximumStringLength.Value78 => 6,
            MaximumStringLength.Value142 => 7,
            MaximumStringLength.Value255 => 8,
            _ => throw new InvalidDataException(Resources.Get("InternalInvalidStringLengthOption"))};
        int length = reader.Read(width) + 13;
        if (length > (int)maximum - 2)
        {
            throw new InvalidDataException(Resources.Get("InternalInvalidStringExtensionLength"));
        }

        return length;
    }
}
