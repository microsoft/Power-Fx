// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PowerFxTest
    {
        static PowerFxTest()
        {
            // Force all tests to use en-US locale
            PowerFxConfig.CultureOverride = new CultureInfo("en-US");
        }
    }
}
