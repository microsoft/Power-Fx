// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    /// <summary>
    /// Static class used to store built in Power Fx enums.
    /// </summary>
    internal sealed class EnumStoreBuilder
    {
        #region Default Enums
        private static IReadOnlyDictionary<string, EnumSymbol> DefaultEnumSymbols { get; } =
            new Dictionary<string, EnumSymbol>()
            {
                { LanguageConstants.ColorEnumString, BuiltInEnums.ColorEnum },
                { LanguageConstants.DateTimeFormatEnumString, BuiltInEnums.DateTimeFormatEnum },
                { LanguageConstants.StartOfWeekEnumString, BuiltInEnums.StartOfWeekEnum },
                { LanguageConstants.SortOrderEnumString, BuiltInEnums.SortOrderEnum },
                { LanguageConstants.TimeUnitEnumString, BuiltInEnums.TimeUnitEnum },
                { LanguageConstants.MatchOptionsEnumString, BuiltInEnums.MatchOptionsEnum },
                { LanguageConstants.MatchEnumString, BuiltInEnums.MatchEnum },
                { LanguageConstants.ErrorKindEnumString, BuiltInEnums.ErrorKindEnum },
                { LanguageConstants.JSONFormatEnumString, BuiltInEnums.JSONFormatEnum },
                { LanguageConstants.TraceSeverityEnumString, BuiltInEnums.TraceSeverityEnum },
                { LanguageConstants.TraceOptionsEnumString, BuiltInEnums.TraceOptionsEnum },
            };

        // DefaultEnums is legacy and only used by Power Apps
        internal static IReadOnlyDictionary<string, string> DefaultEnums { get; } =
            new Dictionary<string, string>()
            {
                {
                    LanguageConstants.ColorEnumString,
                    ColorTable.ToString()
                },
                {
                    LanguageConstants.DateTimeFormatEnumString,
                    "%s[LongDate:\"'longdate'\", ShortDate:\"'shortdate'\", LongTime:\"'longtime'\", ShortTime:\"'shorttime'\", LongTime24:\"'longtime24'\", " +
                    "ShortTime24:\"'shorttime24'\", LongDateTime:\"'longdatetime'\", ShortDateTime:\"'shortdatetime'\", " +
                    "LongDateTime24:\"'longdatetime24'\", ShortDateTime24:\"'shortdatetime24'\", UTC:\"utc\"]"
                },
                {
                    LanguageConstants.StartOfWeekEnumString,
                    "%n[Sunday:1, Monday:2, MondayZero:3, Tuesday:12, Wednesday:13, Thursday:14, Friday:15, Saturday:16]"
                },
                {
                    LanguageConstants.SortOrderEnumString,
                    "%s[Ascending:\"ascending\", Descending:\"descending\"]"
                },
                {
                    LanguageConstants.TimeUnitEnumString,
                    "%s[Years:\"years\", Quarters:\"quarters\", Months:\"months\", Days:\"days\", Hours:\"hours\", Minutes:\"minutes\", Seconds:\"seconds\", Milliseconds:\"milliseconds\"]"
                },
                {
                    LanguageConstants.ErrorKindEnumString,
                    "%n[None:0, Sync:1, MissingRequired:2, CreatePermission:3, EditPermissions:4, DeletePermissions:5, Conflict:6, NotFound:7, " +
                    "ConstraintViolated:8, GeneratedValue:9, ReadOnlyValue:10, Validation: 11, Unknown: 12, Div0: 13, BadLanguageCode: 14, " +
                    "BadRegex: 15, InvalidFunctionUsage: 16, FileNotFound: 17, AnalysisError: 18, ReadPermission: 19, NotSupported: 20, " +
                    "InsufficientMemory: 21, QuotaExceeded: 22, Network: 23, Numeric: 24, InvalidArgument: 25, Internal: 26, NotApplicable: 27, Timeout: 28, ServiceUnavailable:29, Custom: 1000]"
                },
                {
                    LanguageConstants.MatchOptionsEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.MatchOptionsEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                },
                {
                    LanguageConstants.MatchEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.MatchEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                },
                {
                    LanguageConstants.JSONFormatEnumString,
                    "%s[Compact:\"\", IndentFour:\"4\", IgnoreBinaryData:\"G\", IncludeBinaryData:\"B\", IgnoreUnsupportedTypes:\"I\", FlattenValueTables:\"_\"]"
                },
                {
                    LanguageConstants.TraceSeverityEnumString,
                    $"%n[{string.Join(", ", BuiltInEnums.TraceSeverityEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}: {pair.Value.Object}"))}]"
                },
                {
                    LanguageConstants.TraceOptionsEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.TraceOptionsEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                }
            };
        #endregion

        private readonly Dictionary<string, EnumSymbol> _enumSymbols = new Dictionary<string, EnumSymbol>();

        #region Internal methods
        internal EnumStoreBuilder WithRequiredEnums(TexlFunctionSet functions)
        {
            foreach (var name in functions.Enums)
            {
                if (!_enumSymbols.ContainsKey(name))
                {
                    if (!DefaultEnumSymbols.TryGetValue(name, out var enumSymbol))
                    {
                        throw new InvalidOperationException($"Could not find enum {name}");
                    }

                    _enumSymbols.Add(name, enumSymbol);
                }
            }

            return this;
        }

        internal EnumStoreBuilder WithDefaultEnums()
        {
            foreach (var defaultEnum in DefaultEnumSymbols)
            {
                if (!_enumSymbols.ContainsKey(defaultEnum.Key))
                {
                    _enumSymbols.Add(defaultEnum.Key, defaultEnum.Value);
                }
            }

            return this;
        }

        internal EnumStore Build()
        {
            return EnumStore.Build(_enumSymbols.Values.ToList());
        }
        #endregion

        // Do not use, only for testing
        internal EnumStoreBuilder TestOnly_WithCustomEnum(EnumSymbol custom, bool append = false)
        {
            if (!append)
            {
                _enumSymbols.Clear();
            }

            _enumSymbols.Add(custom.EntityName, custom);
            return this;
        }
    }
}
