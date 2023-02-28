// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Xunit;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Tests
{
    public class FunctionDefinitionTests : PowerFxTest
    {
        [Fact]
        public void TabularOverloadListIsOverloadOfSingleFunction()
        {
            var type = typeof(Functions.Library);
            var simpleFunctions = type.GetProperty("SimpleFunctionImplementations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
                as IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr>;
            Assert.NotNull(simpleFunctions);
            var tabularOverloads = type.GetProperty("SimpleFunctionTabularOverloadImplementations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
                as IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr>;
            Assert.NotNull(tabularOverloads);
            var tabularMultiArgsOverloads = type.GetProperty("SimpleFunctionMultiArgsTabularOverloadImplementations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
                as IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr>;
            Assert.NotNull(tabularMultiArgsOverloads);

            ValidateOverloads(simpleFunctions, tabularOverloads, isMultiArg: false);
            ValidateOverloads(simpleFunctions, tabularMultiArgsOverloads, isMultiArg: true);
        }

        [Fact]
        public void ValidateNoMutation()
        {
            int libraryCount = BuiltinFunctionsCore.BuiltinFunctionsLibrary.Count();
            int testOnlyCount = BuiltinFunctionsCore.TestOnly_AllBuiltinFunctions.Count();

            Assert.Equal(libraryCount, BuiltinFunctionsCore.BuiltinFunctionsLibrary.ToList().Count());
            Assert.True(testOnlyCount > libraryCount);
        }

        private void ValidateOverloads(IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> simpleFunctions, IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> tabularOverloads, bool isMultiArg) 
        {
            foreach (var tabularOverload in tabularOverloads.Keys)
            {
                var simpleFunction = simpleFunctions.First(f => f.Key.Name == tabularOverload.Name).Key;
                Assert.NotNull(simpleFunction);

                // Validate input / output types - simple function should take / return scalars
                Assert.NotEqual(DKind.Table, simpleFunction.ReturnType.Kind);
                Assert.DoesNotContain(simpleFunction.ParamTypes, t => t.Kind == DKind.Table); // No tabular inputs

                // Validate tabular function should return table
                Assert.Equal(DKind.Table, tabularOverload.ReturnType.Kind); // Returns table

                // Validate input types - Single arg Tabular function should take at lease one table
                // Multi arg Tabular Function can have scalar/tabular as arg hence skip this test.
                if (!isMultiArg)
                {
                    Assert.Contains(tabularOverload.ParamTypes, t => t.Kind == DKind.Table); // At least one table input
                }

                // Validate similar arity
                Assert.Equal(tabularOverload.MinArity, simpleFunction.MinArity);
                Assert.Equal(tabularOverload.MaxArity, simpleFunction.MaxArity);
            }
        }
    }
}
