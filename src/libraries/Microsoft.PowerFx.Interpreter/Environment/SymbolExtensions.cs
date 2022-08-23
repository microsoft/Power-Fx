// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.PowerFx
{
    public static class SymbolExtensions
    {
        /// <summary>
        /// Set TimeZoneInfo in SymbolValues, using TimeZone Id.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the TimeZoneInfo.</param>
        /// <param name="timeZoneId">TimeZone Id, like "Pacific Standard Time", "Romance Standard Time"...</param>
        /// <exception cref="TimeZoneNotFoundException">Will throw if timeZoneId is not found.</exception>
        /// <exception cref="ArgumentException">When timeZoneId is null or made of spaces.</exception>
        /// <exception cref="ArgumentNullException">When symbols is null.</exception>
        public static void SetTimeZoneById(this SymbolValues symbols, string timeZoneId)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new ArgumentException("timeZoneId is empty", nameof(timeZoneId));
            }

            symbols.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }

        /// <summary>
        /// Set TimeZoneInfo in SymbolValues, using TimeZone DisplayName.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the TimeZoneInfo.</param>
        /// <param name="timeZoneDisplayName">TimeZone DisplayName, like "(UTC-08:00) Pacific Time (US &amp; Canada)", "(UTC+01:00) Brussels, Copenhagen, Madrid, Paris"...</param>
        /// <exception cref="ArgumentException">When timeZoneDisplayName is null or made of spaces.</exception>
        /// <exception cref="ArgumentNullException">When symbols is null.</exception>
        /// <remarks>If the TimeZone DisplayName is not valid, the TimeZoneInfo will not be set.</remarks>
        public static void SetTimeZoneByDisplayName(this SymbolValues symbols, string timeZoneDisplayName)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (string.IsNullOrWhiteSpace(timeZoneDisplayName))
            {
                throw new ArgumentException("timeZoneDisplayName is empty", nameof(timeZoneDisplayName));
            }

            var tzi = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(tzi => tzi.DisplayName.Equals(timeZoneDisplayName, StringComparison.OrdinalIgnoreCase));

            if (tzi != null)
            {
                symbols.SetTimeZone(tzi);
            }
        }

        /// <summary>
        /// Set TimeZoneInfo.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the TimeZoneInfo.</param>
        /// <param name="timezone">TimeZoneInfo to set.</param>
        /// <exception cref="ArgumentNullException">When timezone is null.</exception>
        public static void SetTimeZone(this SymbolValues symbols, TimeZoneInfo timezone)
        {
            symbols.AddService(timezone ?? throw new ArgumentNullException(nameof(timezone)));
        }

        /// <summary>
        /// Set CultureInfo.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the CultureInfo.</param>
        /// <param name="culture">CultureInfo to set.</param>
        /// <exception cref="ArgumentNullException">When culture is null.</exception>
        public static void SetCulture(this SymbolValues symbols, CultureInfo culture)
        {
            symbols.AddService(culture ?? throw new ArgumentNullException(nameof(culture)));
        }
    }
}
