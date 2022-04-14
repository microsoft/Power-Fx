// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.PowerFx.Core.Tests
{
    public abstract class PowerFxTest
    {
        public PowerFxTest()
        {
            // Ensure all tests are run with en-US locale
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
        }
    }
}
