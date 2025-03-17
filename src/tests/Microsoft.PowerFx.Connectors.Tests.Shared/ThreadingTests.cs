// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    // Do static analysis to look for potential threading issues. 
    public class ThreadingTests
    {
        [Fact]
        public void CheckConnector()
        {
            var asm = typeof(OpenApiParser).Assembly;
            var bugsFieldType = new HashSet<Type>();
            var bugNames = new HashSet<string>()
            {
                "ConnectorFunction._slash",
                "ConnectorFunction._validFunctions",
                "ColumnCapabilities.DefaultFilterFunctionSupport"
            };

            AnalyzeThreadSafety.CheckStatics(asm, bugsFieldType, bugNames);
        }

        [Fact]
        public void CheckImmutableTypeInConnector()
        {
            var assemblies = new Assembly[]
            {
                typeof(OpenApiParser).Assembly
            };

            // https://github.com/microsoft/Power-Fx/issues/1561
            // These types are marked as [ThreadSafeImmutable], but they fail the enforcement checks. 
            var knownFailures = new HashSet<Type>
            {
                typeof(ConnectorSettings)
            };

            AnalyzeThreadSafety.CheckImmutableTypes(assemblies, knownFailures);
        }
    }
}
