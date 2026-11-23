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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Gurux.Compression.Properties;

namespace Gurux.Compression
{
    /// <summary>Configures dictionary bounds, bit widths, string extension limits and diagnostics.</summary>
    public sealed class GXv44Options
    {
        /// <summary>
        /// Initial codeword width in bits, before any STEPUP control.
        /// </summary>
        private int _defaultCodewordSize = 6;

        /// <summary>
        /// Initial ordinal width in bits, before any STEPUP control.
        /// </summary>
        private int _defaultOrdinalSize = 7;

        /// <summary>
        /// Default codeword size.
        /// </summary>
        /// <remarks>
        /// In standard : C2
        /// </remarks>
        [DefaultValue(6)]
        [Range(6, 16, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = nameof(Resources.CodewordSizeRange))]
        public int DefaultCodewordSize
        {
            get
            {
                return _defaultCodewordSize;
            }

            set
            {
                if (value < 6 || value > 16)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.CodewordSizeRange);
                }

                _defaultCodewordSize = value;
            }
        }

        /// <summary>
        /// Default ordinal size.
        /// </summary>
        /// <remarks>
        /// In standard : C5
        /// </remarks>
        [DefaultValue(7)]
        public int DefaultOrdinalSize
        {
            get
            {
                return _defaultOrdinalSize;
            }

            set
            {
                if (value < 7 || value > 8)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.Get("OrdinalSizeRange"));
                }

                _defaultOrdinalSize = value;
            }
        }

        /// <summary>
        /// Gets the maximum number of codewords that can be processed.
        /// </summary>
        public UInt16 MaxCodewords { get; set; } = 1024; // N2

        /// <summary>
        ///  The maximum string length.
        /// </summary>
        /// <remarks>
        ///  N7T is count from this.
        ///  </remarks>
        public MaximumStringLength MaximumStringLength { get; set; } = MaximumStringLength.Value255;

        /// <summary>Gets or sets the maximum history bytes between reinitializations; at least 512.</summary>
        public int HistoryLimit { get; set; } = 3072; // N8

        /// <summary>
        /// Gets or sets a value indicating whether the debug values are shown as a hex string.
        /// </summary>
        public bool Hex { get; set; }

        /// <summary>Validates and clones options to isolate a codec from later caller mutations.</summary>
        /// <returns>A validated independent copy of these options.</returns>
        internal GXv44Options Snapshot()
        {
            Validate();
            return (GXv44Options)MemberwiseClone();
        }

        /// <summary>Checks the configured dictionary, history and supported string-length limits.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The dictionary has fewer than 256 entries, the history has fewer than 512 bytes, or the maximum string length is unsupported.</exception>
        public void Validate()
        {
            if (MaxCodewords < 256)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxCodewords));
            }

            if (!Enum.IsDefined(MaximumStringLength))
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumStringLength));
            }

            if (HistoryLimit < 512)
            {
                throw new ArgumentOutOfRangeException(nameof(HistoryLimit));
            }
        }
    }
}
