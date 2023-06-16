// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

extern alias PfxCore;

using System;
using PfxCore.Microsoft.PowerFx;
using PfxCore.Microsoft.PowerFx.Core.Types;
using PfxCore.Microsoft.PowerFx.Core.Types.Enums;
using PfxCore.Microsoft.PowerFx.Core.Utils;
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
