// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net.Http;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public static class RuntimeConfigExtensions
    {
        public static RuntimeConfig AddTestRuntimeContext(this RuntimeConfig runtimeConfig, string @namespace, HttpMessageInvoker client, bool? throwOnError = null, ITestOutputHelper console = null, bool includeDebug = false)
        {
            TestConnectorRuntimeContext runtimeContext = new TestConnectorRuntimeContext(runtimeConfig, console, includeDebug);

            runtimeContext.Add(@namespace, client, throwOnError);
            runtimeConfig.AddRuntimeContext(runtimeContext);

            return runtimeConfig;
        }
    }
}
