// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class EnumSyncTests : PowerFxTest
    {
        [Fact]
        public void ErrorKindEnumSyncTest()
        {
            Assert.True(DType.TryParse(EnumStoreBuilder.DefaultEnums[LanguageConstants.ErrorKindEnumString], out var dtype));

            var enumNames = Enum.GetNames(typeof(ErrorKind));
            var enumValues = Enum.GetValues(typeof(ErrorKind));

            for (int i = 0; i < enumNames.Length; i++)
            {
                Assert.True(dtype.TryGetEnumValue(new DName(enumNames[i]), out var enumValue), $"DType doesn't contain {enumNames[i]}");
                Assert.Equal((int)enumValues.GetValue(i), Convert.ToInt32(enumValue));
            }
        }

        [Fact]
        public void DefaultEnumsSyncTest()
        {
            var kindStrings = new Dictionary<DKind, string>() { { DKind.Number, "%n[" }, { DKind.Color, "%c[" }, { DKind.String, "%s[" } };

            void Test(string enumString, EnumSymbol enumSymbol)
            {
                Assert.NotNull(enumString);
                Assert.True(kindStrings.ContainsKey(enumSymbol.BackingKind));
                Assert.StartsWith(kindStrings[enumSymbol.BackingKind], enumString);

                Assert.True(DType.TryParse(enumString, out var dtype));
                Assert.True(enumSymbol.BackingKind == dtype.EnumSuperkind);

                foreach (var enumSymbolOption in enumSymbol.OptionNames)
                {
                    Assert.True(dtype.TryGetEnumValue(new DName(enumSymbolOption), out var enumValue), $"Enum doesn't contain {enumSymbolOption.Value}");
                    Assert.True(enumSymbol.EnumType.TryGetEnumValue(new DName(enumSymbolOption), out var enumSymbolValue), $"EnumSymbol doesn't contain {enumSymbolOption.Value}");

                    // quotes are escaped in enumSpecs
                    if (enumSymbolValue is string stringValue)
                    {
                        enumSymbolValue = stringValue.Replace(@"""", @"""""");
                    }

                    Assert.True((enumValue is double && enumSymbolValue is double && (enumSymbol.BackingKind == DKind.Color || enumSymbol.BackingKind == DKind.Number)) ||
                                (enumValue is string && enumSymbolValue is string && enumSymbol.BackingKind == DKind.String));
                    Assert.Equal(enumValue, enumSymbolValue);
                }

                foreach (var enumPair in dtype.ValueTree.GetPairs())
                {
                    Assert.Contains(new DName(enumPair.Key), enumSymbol.OptionNames);
                }
            }

            foreach (var enumSymbol in EnumStoreBuilder.DefaultEnumSymbols)
            {
                var enumString = EnumStoreBuilder.DefaultEnums[enumSymbol.Key];

                // Match is a special case, see EnumStoreBuilder.cs for more details
                if (enumSymbol.Key == "Match")
                {
                    Test(EnumStoreBuilder.DefaultEnums_MatchEnumV1, enumSymbol.Value); // tests V1
                    enumString = EnumStoreBuilder.DefaultEnums_MatchEnumV1; // sets up for test of pre-V1
                }

                Test(enumString, enumSymbol.Value);
            }
        }
    }
}
