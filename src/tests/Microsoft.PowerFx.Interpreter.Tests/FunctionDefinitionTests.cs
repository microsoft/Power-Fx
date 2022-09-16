// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
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
                var simpleFunction = simpleFunctions.First(f => f.Key.Name == tabularOverload.Name).Key;
                Assert.NotNull(simpleFunction);

                // Validate input / output types - simple function should take / return scalars
                Assert.NotEqual(DKind.Table, simpleFunction.ReturnType.Kind);
                Assert.DoesNotContain(simpleFunction.ParamTypes, t => t.Kind == DKind.Table); // No tabular inputs

                // Validate input / output types - tabular function should take at least one table / return table
                Assert.Equal(DKind.Table, tabularOverload.ReturnType.Kind); // Returns table
                Assert.Contains(tabularOverload.ParamTypes, t => t.Kind == DKind.Table); // At least one table input

                // Validate similar arity
                Assert.Equal(tabularOverload.MinArity, simpleFunction.MinArity);
                Assert.Equal(tabularOverload.MaxArity, simpleFunction.MaxArity);
            }
        }
    }
}
