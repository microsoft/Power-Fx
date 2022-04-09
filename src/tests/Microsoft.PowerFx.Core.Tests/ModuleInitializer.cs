// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.PowerFx.Core.Tests
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void SetLocaleForTests()
        {
            // Ensure all tests are run with English US locale
            // Even if the local machine is using a different one
            var testCulture = CultureInfo.GetCultureInfo("en-US");

            Thread.CurrentThread.CurrentCulture = testCulture;
            Thread.CurrentThread.CurrentUICulture = testCulture;
            CultureInfo.DefaultThreadCurrentCulture = testCulture;
            CultureInfo.DefaultThreadCurrentUICulture = testCulture;
        }
    }
}
