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
    public class ErrorKindEnumTests : PowerFxTest
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
    }
}
