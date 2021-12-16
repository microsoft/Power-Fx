// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestPowerFxScopeFactory : IPowerFxScopeFactory
    {
        public delegate IPowerFxScope GetOrCreateInstanceDelegate(string documentUri);

        private GetOrCreateInstanceDelegate _getOrCreateInstance;

        public TestPowerFxScopeFactory(GetOrCreateInstanceDelegate getOrCreateInstance)
        {
            _getOrCreateInstance = getOrCreateInstance;
        }

        public IPowerFxScope GetOrCreateInstance(string documentUri)
        {
            return _getOrCreateInstance(documentUri);
        }
    }
}
