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
                    "InsufficientMemory: 21, QuotaExceeded: 22, Network: 23, Numeric: 24, InvalidArgument: 25, Internal: 26, NotApplicable: 27, Timeout: 28, Custom: 1000]"
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
                    "%s[Compact:\"\", IndentFour:\"4\", IgnoreBinaryData:\"G\", IncludeBinaryData:\"B\", IgnoreUnsupportedTypes:\"I\"]"
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

        private readonly Dictionary<string, string> _workingEnums = new Dictionary<string, string>();

        private ImmutableList<EnumSymbol> _enumSymbols;

        private Dictionary<string, DType> _enumTypes;

        #region Internal methods
        internal EnumStoreBuilder WithRequiredEnums(TexlFunctionSet functions)
        {
            var missingEnums = new Dictionary<string, string>();

            foreach (var name in functions.Enums)
            {
                if (!_workingEnums.ContainsKey(name) && !missingEnums.ContainsKey(name))
                {
                    if (!DefaultEnums.TryGetValue(name, out var enumName))
                    {
                        throw new InvalidOperationException($"Could not find enum {name}");
                    }

                    missingEnums.Add(name, enumName);
                }
            }

            foreach (var missingEnum in missingEnums)
            {
                _workingEnums.Add(missingEnum.Key, missingEnum.Value);
            }

            return this;
        }

        internal EnumStoreBuilder WithDefaultEnums()
        {
            foreach (var defaultEnum in DefaultEnums)
            {
                if (!_workingEnums.ContainsKey(defaultEnum.Key))
                {
                    _workingEnums.Add(defaultEnum.Key, defaultEnum.Value);
                }
            }

            return this;
        }

        internal EnumStore Build()
        {
            return EnumStore.Build(new List<EnumSymbol>(EnumSymbols));
        }
        #endregion

        private Dictionary<string, DType> RegenerateEnumTypes()
        {
            var enumTypes = _workingEnums.ToDictionary(enumSpec => enumSpec.Key, enumSpec =>
            {
                DType.TryParse(enumSpec.Value, out var type).Verify();
                return type;
            });

            return enumTypes;
        }

        private IEnumerable<(DName name, DType typeSpec)> Enums()
        {
            CollectionUtils.EnsureInstanceCreated(ref _enumTypes, () =>
            {
                return RegenerateEnumTypes();
            });

            var list = ImmutableList.CreateBuilder<(DName name, DType typeSpec)>();
            foreach (var enumSpec in _workingEnums)
            {
                Contracts.Assert(DName.IsValidDName(enumSpec.Key));

                var name = new DName(enumSpec.Key);
                list.Add((name, _enumTypes[enumSpec.Key]));
            }

            return list.ToImmutable();
        }

        private IEnumerable<EnumSymbol> EnumSymbols =>
            CollectionUtils.EnsureInstanceCreated(
                    ref _enumSymbols, () => RegenerateEnumSymbols());

        private ImmutableList<EnumSymbol> RegenerateEnumSymbols()
        {
            var list = ImmutableList.CreateBuilder<EnumSymbol>();
            foreach (var (name, typeSpec) in Enums())
            {
                list.Add(new EnumSymbol(name, typeSpec));
            }

            return list.ToImmutable();
        }

        // Do not use, only for testing
        internal EnumStoreBuilder TestOnly_WithCustomEnum(EnumSymbol custom)
        {
            _enumSymbols = ImmutableList.CreateRange(new List<EnumSymbol>() { custom });
            return this;
        }
    }
}
