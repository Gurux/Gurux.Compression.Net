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
    /// Represents the set of control codes used to indicate specific states actions 
    /// during the execution of an operation, such as compression or data processing.
    /// </summary>
    public enum Operation
    {
        /// <summary>No preceding data token is available after dictionary initialization.</summary>
        None = -3,

        /// <summary>
        /// Compression or decompression is started. 
        /// </summary>
        Start = -2,

        /// <summary>
        /// Compression or decompression is ended. 
        /// </summary>
        End = -1,

        /// <summary>
        /// Ordinal value is appended.
        /// </summary>
        Ordinal = 0,

        /// <summary>
        /// The corresponding character is appended to the 
        /// previous string to create a longer string.
        /// </summary>
        Codeword = 1,

        /// <summary>
        /// The corresponding character is not appended 
        /// to the extended string. 
        /// </summary>
        StringExtension = 2,

        /// <summary>
        /// Increases the ordinal size.
        /// </summary>
        /// <remarks>
        /// The new value doesn't fit into the current ordinal size.
        /// </remarks>
        StepUpOrdinal,

        /// <summary>
        /// Increases the codeword size.
        /// </summary>
        /// <remarks>
        /// The new value doesn't fit into the current codeword size.
        /// </remarks>
        StepUpCodeword,

        /// <summary>
        /// Compression is finished.
        /// </summary>
        Flush,

        /// <summary>
        /// Force reinitialization of dictionaries.
        /// </summary>
        ReInit,

        /// <summary>
        /// An error occurred during the execution of an operation.
        /// </summary>
        Error,

        /// <summary>
        /// Codeword that encoder or decoder is currently processing.
        /// </summary>
        Processing,
    }
}
