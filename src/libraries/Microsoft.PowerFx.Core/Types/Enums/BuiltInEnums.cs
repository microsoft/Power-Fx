// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal static class BuiltInEnums
    {
        public static EnumSymbol ColorEnum = new EnumSymbol(
            new DName(EnumConstants.ColorEnumString),
            DType.Color,
            ColorTable.InvariantNameToHexMap.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)));

        public static EnumSymbol StartOfWeekEnum = new EnumSymbol(
            new DName(EnumConstants.StartOfWeekEnumString),
            DType.Number,
            new Dictionary<string, object>()
            {
                { "Sunday", 1 },
                { "Monday", 2 },
                { "MondayZero", 3 },
                { "Tuesday", 12 },
                { "Wednesday", 13 },
                { "Thursday", 14 },
                { "Friday", 15 },
                { "Saturday", 16 },
            });

        public static EnumSymbol DateTimeFormatEnum = new EnumSymbol(
            new DName(EnumConstants.DateTimeFormatEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "LongDate", "'longdate'" },
                { "ShortDate", "'shortdate'" },
                { "LongTime", "'longtime'" },
                { "ShortTime", "'shorttime'" },
                { "LongTime24", "'longtime24'" },
                { "ShortTime24", "'shorttime24'" },
                { "LongDateTime", "'longdatetime'" },
                { "ShortDateTime", "'shortdatetime'" },
                { "LongDateTime24", "'longdatetime24'" },
                { "ShortDateTime24", "'shortdatetime24'" },
                { "UTC", "utc" }
            });

        public static EnumSymbol TimeUnitEnum = new EnumSymbol(
            new DName(EnumConstants.TimeUnitEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Years", "years" },
                { "Quarters", "quarters" },
                { "Months", "months" },
                { "Days", "days" },
                { "Hours", "hours" },
                { "Minutes", "minutes" },
                { "Seconds", "seconds" },
                { "Milliseconds", "milliseconds" },
            });

        public static EnumSymbol SortOrderEnum = new EnumSymbol(
            new DName(LanguageConstants.SortOrderEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Ascending", "ascending" },
                { "Descending", "descending" },
            });
    }
}
