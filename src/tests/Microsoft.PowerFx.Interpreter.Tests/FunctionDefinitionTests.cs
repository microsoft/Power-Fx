// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Tests
{
    public class FunctionDefinitionTests : PowerFxTest
    {
        [Fact]
        public void TabularOverloadListIsOverloadOfSingleFunction()
        {
            var type = typeof(Microsoft.PowerFx.Functions.Library);
            var simpleFunctions = type.GetProperty("SimpleFunctionImplementations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
                as IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr>;
            Assert.NotNull(simpleFunctions);
            var tabularOverloads = type.GetProperty("SimpleFunctionTabularOverloadImplementations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
                as IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr>;
            Assert.NotNull(tabularOverloads);

            foreach (var tabularOverload in tabularOverloads.Keys)
            {
                Assert.Contains(simpleFunctions, f => f.Key.Name == tabularOverload.Name);
            }
        }
    }
}
