// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Public.Logging;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal static class BuiltInEnums
    {
        public static readonly EnumSymbol ColorEnum = new EnumSymbol(
            new DName(LanguageConstants.ColorEnumString),
            DType.Color,
            ColorTable.InvariantNameToHexMap.Select(kvp => new KeyValuePair<string, object>(kvp.Key, Convert.ToDouble(kvp.Value))));

        public static readonly EnumSymbol StartOfWeekEnum = new EnumSymbol(
            new DName(LanguageConstants.StartOfWeekEnumString),
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

        public static readonly EnumSymbol DateTimeFormatEnum = new EnumSymbol(
            new DName(LanguageConstants.DateTimeFormatEnumString),
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

        public static readonly EnumSymbol TimeUnitEnum = new EnumSymbol(
            new DName(LanguageConstants.TimeUnitEnumString),
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

        public static readonly EnumSymbol SortOrderEnum = new EnumSymbol(
            new DName(LanguageConstants.SortOrderEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Ascending", "ascending" },
                { "Descending", "descending" },
            });

        public static readonly EnumSymbol MatchOptionsEnum = new EnumSymbol(new DName(LanguageConstants.MatchOptionsEnumString), DType.String, new Dictionary<string, object>()
        {
            { "BeginsWith", "^c" },
            { "EndsWith", "$c" },
            { "Complete", "^c$" },
            { "Contains", "c" },
            { "IgnoreCase", "i" },
            { "Multiline", "m" }
        });

        public static readonly EnumSymbol MatchEnum = new EnumSymbol(new DName(LanguageConstants.MatchEnumString), DType.String, new Dictionary<string, object>()
        {
            { "Any", "." },
            { "Comma", "," },
            { "Digit", "\\d" },
            { "Email", ".+@.+\\.[^.]{2,}" },
            { "Hyphen", "\\-" },
            { "LeftParen", "\\(" },
            { "Letter", "\\p{L}" },
            { "MultipleDigits", "\\d+" },
            { "MultipleLetters", "\\p{L}+" },
            { "MultipleNonSpaces", "\\S+" },
            { "MultipleSpaces", "\\s+" },
            { "NonSpace", "\\S" },
            { "OptionalDigits", "\\d*" },
            { "OptionalLetters", "\\p{L}*" },
            { "OptionalNonSpaces", "\\S*" },
            { "OptionalSpaces", "\\s*" },
            { "Period", "\\." },
            { "RightParen", "\\)" },
            { "Space", "\\s" },
            { "Tab", "\\t" }
        });

        public static readonly EnumSymbol TraceSeverityEnum = new EnumSymbol(new DName(LanguageConstants.TraceSeverityEnumString), DType.Number, TraceSeverityDictionary());

        private static Dictionary<string, object> TraceSeverityDictionary()
        {
            var traceDictionary = new Dictionary<string, object>();
            foreach (TraceSeverity severity in Enum.GetValues(typeof(TraceSeverity)))
            {
                traceDictionary[severity.ToString()] = (int)severity;
            }

            return traceDictionary;
        }

        public static readonly EnumSymbol TraceOptionsEnum = new EnumSymbol(new DName(LanguageConstants.TraceOptionsEnumString), DType.String, new Dictionary<string, object>()
        {
            { "None", "n" },
            { "IgnoreUnsupportedTypes", TraceFunction.IgnoreUnsupportedTypesEnumValue },
        });
    }
}
