﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestPowerFxScopeFactory : IPowerFxScopeFactory
    {
        public delegate IPowerFxScope GetOrCreateInstanceDelegate(string documentUri);

        private readonly GetOrCreateInstanceDelegate _getOrCreateInstance;

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
