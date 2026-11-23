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
using System.Globalization;
using System.Resources;

namespace Gurux.Compression.Properties
{
    /// <summary>Reads the library's localized validation, cancellation and decoding messages.</summary>
    internal static class Resources
    {
        /// <summary>Resolves the embedded neutral resources and optional culture-specific satellites.</summary>
        private static readonly ResourceManager _manager = new ResourceManager("Gurux.Compression.Properties.Resources", typeof(Resources).Assembly);

        /// <summary>Gets the codeword-width validation message used by data annotations.</summary>
        public static string CodewordSizeRange => Get(nameof(CodewordSizeRange));

        /// <summary>Finds a complete message using the current user-interface culture.</summary>
        /// <param name="key">The resource key identifying the message.</param>
        /// <returns>The localized message, falling back to the neutral English resource.</returns>
        /// <exception cref="MissingManifestResourceException">The requested message is not present in the embedded resources.</exception>
        public static string Get(string key)
        {
            return _manager.GetString(key, CultureInfo.CurrentUICulture) ?? throw new MissingManifestResourceException(key);
        }

        /// <summary>Formats a localized complete message using the current formatting culture.</summary>
        /// <param name="key">The resource key identifying a format string with numbered placeholders.</param>
        /// <param name="arguments">Values substituted into the numbered placeholders.</param>
        /// <returns>The localized message with its values inserted.</returns>
        /// <exception cref="MissingManifestResourceException">The format resource cannot be found.</exception>
        /// <exception cref="FormatException">The resource placeholders do not match the arguments.</exception>
        public static string Format(string key, params object? [] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
        }
    }
}
