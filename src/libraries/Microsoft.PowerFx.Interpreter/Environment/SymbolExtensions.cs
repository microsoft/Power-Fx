// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static void SetTimeZoneById(this SymbolValues symbols, string timeZoneId)
        {            
            symbols.AddService(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }        

        /// <summary>
        /// Set TimeZoneInfo in SymbolValues, using TimeZone DisplayName.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the TimeZoneInfo.</param>
        /// <param name="timeZoneDisplayName">TimeZone DisplayName, like "(UTC-08:00) Pacific Time (US &amp; Canada)", "(UTC+01:00) Brussels, Copenhagen, Madrid, Paris"...</param>
        /// <remarks>If the TimeZone DisplayName is not valid, the TimeZoneInfo will not be set.</remarks>
        public static void SetTimeZoneByDisplayName(this SymbolValues symbols, string timeZoneDisplayName)
        {
            var tzi = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(tzi => tzi.DisplayName.Equals(timeZoneDisplayName, StringComparison.OrdinalIgnoreCase));

            if (tzi != null)
            {
                symbols.AddService(tzi);
            }
        }
    }
}
