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
            // We would like to use ModuleInitializer, but that requires .Net 5 and the tests can't run on that version yet.
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.moduleinitializerattribute?view=net-6.0
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
        }
    }
}
