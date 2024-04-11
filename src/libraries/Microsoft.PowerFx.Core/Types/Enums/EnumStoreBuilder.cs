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

        // DefaultEnums, with enum strings, is legacy and only used by Power Apps
        internal static IReadOnlyDictionary<string, string> DefaultEnums { get; } =
            new Dictionary<string, string>()
            {
                {
                    LanguageConstants.ColorEnumString,
                    ColorTable.ToString()
                },
                {
                    LanguageConstants.DateTimeFormatEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.DateTimeFormatEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                },
                {
                    LanguageConstants.StartOfWeekEnumString,
                    $"%n[{string.Join(", ", BuiltInEnums.StartOfWeekEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}: {pair.Value.Object}"))}]"
                },
                {
                    LanguageConstants.SortOrderEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.SortOrderEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                },
                {
                    LanguageConstants.TimeUnitEnumString,
                    $"%s[{string.Join(", ", BuiltInEnums.TimeUnitEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
                },
                {
                    LanguageConstants.ErrorKindEnumString,
                    $"%n[{string.Join(", ", BuiltInEnums.ErrorKindEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:{pair.Value.Object}"))}]"
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
                    $"%s[{string.Join(", ", BuiltInEnums.JSONFormatEnum.EnumType.ValueTree.GetPairs().Select(pair => $@"{pair.Key}:""{pair.Value.Object}"""))}]"
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
