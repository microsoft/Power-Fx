// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Logging;

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

        public static readonly EnumSymbol MatchOptionsEnum = new EnumSymbol(
            new DName(LanguageConstants.MatchOptionsEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "BeginsWith", "^c" },
                { "EndsWith", "$c" },
                { "Complete", "^c$" },
                { "Contains", "c" },
                { "IgnoreCase", "i" },
                { "Multiline", "m" }
            });

        public static readonly EnumSymbol MatchEnum = new EnumSymbol(
            new DName(LanguageConstants.MatchEnumString),
            DType.String,
            new Dictionary<string, object>()
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

        public static readonly EnumSymbol ErrorKindEnum = new EnumSymbol(
            new DName(LanguageConstants.ErrorKindEnumString),
            DType.Number,
            new Dictionary<string, object>()
            {
                { "None", 0 },
                { "Sync", 1 },
                { "MissingRequired", 2 },
                { "CreatePermission", 3 },
                { "EditPermissions", 4 },
                { "DeletePermissions", 5 },
                { "Conflict", 6 },
                { "NotFound", 7 },
                { "ConstraintViolated", 8 },
                { "GeneratedValue", 9 },
                { "ReadOnlyValue", 10 },
                { "Validation", 11 },
                { "Unknown", 12 },
                { "Div0", 13 },
                { "BadLanguageCode", 14 },
                { "BadRegex", 15 },
                { "InvalidFunctionUsage", 16 },
                { "FileNotFound", 17 },
                { "AnalysisError", 18 },
                { "ReadPermission", 19 },
                { "NotSupported", 20 },
                { "InsufficientMemory", 21 },
                { "QuotaExceeded", 22 },
                { "Network", 23 },
                { "Numeric", 24 },
                { "InvalidArgument", 25 },
                { "Internal", 26 },
                { "NotApplicable", 27 },
                { "Timeout", 28 },
                { "ServiceUnavailable", 29 },
                { "Custom", 1000 },
            });

        public static readonly EnumSymbol JSONFormatEnum = new EnumSymbol(
            new DName(LanguageConstants.JSONFormatEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "Compact", string.Empty },
                { "IndentFour", "4" },
                { "IgnoreBinaryData", "G" },
                { "IncludeBinaryData", "B" },
                { "IgnoreUnsupportedTypes", "I" },
                { "FlattenValueTables", "_" }
            });

        public static readonly EnumSymbol TraceSeverityEnum = new EnumSymbol(
            new DName(LanguageConstants.TraceSeverityEnumString),
            DType.Number,
            TraceSeverityDictionary());

        private static Dictionary<string, object> TraceSeverityDictionary()
        {
            var traceDictionary = new Dictionary<string, object>();
            foreach (TraceSeverity severity in Enum.GetValues(typeof(TraceSeverity)))
            {
                traceDictionary[severity.ToString()] = (int)severity;
            }

            return traceDictionary;
        }

        public static readonly EnumSymbol TraceOptionsEnum = new EnumSymbol(
            new DName(LanguageConstants.TraceOptionsEnumString),
            DType.String,
            new Dictionary<string, object>()
            {
                { "None", "none" },
                { "IgnoreUnsupportedTypes", TraceFunction.IgnoreUnsupportedTypesEnumValue },
            });
    }
}
