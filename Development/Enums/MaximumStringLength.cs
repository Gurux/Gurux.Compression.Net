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
namespace Gurux.Compression.Enums
{
    /// <summary>
    /// Specifies predefined maximum string lengths that can be used to enforce constraints on string values.
    /// </summary>
    /// <remarks>This enumeration provides a set of commonly used maximum string lengths, such as 46, 78, 142,
    /// and 255 bytes. It can be used to standardize string length constraints across an application.</remarks>
    public enum MaximumStringLength : byte
    {
        /// <summary>
        /// The maximum string length is 46 bytes.
        /// </summary>
        Value46 = 46,

        /// <summary>
        /// The maximum string length is 78 bytes.
        /// </summary>
        Value78 = 78,

        /// <summary>
        /// The maximum string length is 142 bytes.
        /// </summary>
        Value142 = 142,

        /// <summary>
        /// The maximum string length is 255 bytes.
        /// </summary>
        Value255 = 255
    }
}
