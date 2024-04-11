// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
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

            foreach (var enumSymbol in EnumStoreBuilder.DefaultEnumSymbols)
            {
                var enumString = EnumStoreBuilder.DefaultEnums[enumSymbol.Key];
                Assert.NotNull(enumString);
                Assert.True(kindStrings.ContainsKey(enumSymbol.Value.BackingKind));
                Assert.StartsWith(kindStrings[enumSymbol.Value.BackingKind], enumString);

                Assert.True(DType.TryParse(enumString, out var dtype));
                Assert.True(enumSymbol.Value.BackingKind == dtype.EnumSuperkind);

                foreach (var enumSymbolOption in enumSymbol.Value.OptionNames)
                {
                    Assert.True(dtype.TryGetEnumValue(new DName(enumSymbolOption), out var enumValue), $"Enum doesn't contain {enumSymbolOption.Value}");
                    Assert.True(enumSymbol.Value.EnumType.TryGetEnumValue(new DName(enumSymbolOption), out var enumSymbolValue), $"EnumSymbol doesn't contain {enumSymbolOption.Value}");
                    Assert.True((enumValue is double && enumSymbolValue is double && (enumSymbol.Value.BackingKind == DKind.Color || enumSymbol.Value.BackingKind == DKind.Number)) ||
                                (enumValue is string && enumSymbolValue is string && enumSymbol.Value.BackingKind == DKind.String));
                    Assert.Equal(enumValue, enumSymbolValue);
                }
            }
        }
    }
}
