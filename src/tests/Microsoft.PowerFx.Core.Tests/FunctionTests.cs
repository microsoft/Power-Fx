// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FunctionTests
    {
        [Fact]
        public void TestAllBuiltinFunctionsHaveParameterDescriptions()
        {
            var texlFunctionsLibrary = BuiltinFunctionsCore.TestOnly_AllBuiltinFunctions;
            var functions = texlFunctionsLibrary
                .Where(x => !x.FunctionCategoriesMask.HasFlag(FunctionCategories.REST));

            foreach (var function in functions)
            {
                if (function.MaxArity == 0)
                {
                    continue;
                }

                foreach (var paramName in function.GetParamNames())
                {
                    Assert.True(
                        function.TryGetParamDescription(paramName, out var descr),
                        "Missing parameter description. Please add the following to Resources.pares: " + "About" + function.LocaleInvariantName + "_" + paramName);
                }
            }
        }

        [Fact]
        public void TextFunctionSet_Ctor()
        {
            _ = new TexlFunctionSet();
        }

        [Fact]
        public void TextFunctionSet_Ctor_NullFunction()
        {
            Assert.Throws<ArgumentNullException>(() => new TexlFunctionSet((TexlFunction)null));
        }

        [Fact]
        public void TextFunctionSet_Ctor_NullFunction2()
        {
            Assert.Throws<ArgumentNullException>(() => new TexlFunctionSet((IEnumerable<TexlFunction>)null));
        }

        [Fact]
        [Obsolete("Contains testing Obsolete functions")]
        public void TextFunctionSet_OneFunc()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1");
            tfs.Add(func1);

            Assert.Empty(tfs.Enums);
            Assert.Single(tfs.Functions);
            Assert.Contains(tfs.Functions, f => f == func1);
            Assert.Contains(tfs.WithName("func1"), f => f == func1);
            Assert.Contains(tfs.WithInvariantName("func1"), f => f == func1);
            Assert.Empty(tfs.WithName("FUNC1"));
            Assert.Contains(tfs.WithInvariantName("FUNC1"), f => f == func1);
            Assert.True(tfs.AnyWithName("func1"));
            Assert.False(tfs.AnyWithName("FUNC1"));
            Assert.Equal(1, tfs.Count());
            Assert.Single(tfs.FunctionNames);
            Assert.Contains(tfs.FunctionNames, f => f == "func1");
            Assert.Single(tfs.InvariantFunctionNames);
            Assert.Contains(tfs.InvariantFunctionNames, f => f == "func1");            
        }

        [Fact]
        [Obsolete("Contains testing Obsolete functions")]
        public void TextFunctionSet_TwoFunc()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1");
            var func2 = new TestTexlFunction("func2");
            tfs.Add(func1);
            tfs.Add(func2);

            Assert.Empty(tfs.Enums);
            Assert.Equal(2, tfs.Functions.Count());
            Assert.Contains(tfs.Functions, f => f == func1);
            Assert.Contains(tfs.Functions, f => f == func2);
            Assert.Contains(tfs.WithName("func1"), f => f == func1);
            Assert.Contains(tfs.WithName("func2"), f => f == func2);
            Assert.Contains(tfs.WithInvariantName("func1"), f => f == func1);
            Assert.Contains(tfs.WithInvariantName("func2"), f => f == func2);
            Assert.Empty(tfs.WithName("FUNC1"));
            Assert.Empty(tfs.WithName("FUNC2"));
            Assert.Contains(tfs.WithInvariantName("FUNC1"), f => f == func1);
            Assert.Contains(tfs.WithInvariantName("FUNC2"), f => f == func2);
            Assert.True(tfs.AnyWithName("func1"));
            Assert.False(tfs.AnyWithName("FUNC1"));
            Assert.True(tfs.AnyWithName("func2"));
            Assert.False(tfs.AnyWithName("FUNC2"));
            Assert.Equal(2, tfs.Count());
            Assert.Equal(2, tfs.FunctionNames.Count());
            Assert.Equal(2, tfs.InvariantFunctionNames.Count());
            Assert.Contains(tfs.FunctionNames, f => f == "func1");
            Assert.Contains(tfs.FunctionNames, f => f == "func2");            
            Assert.Contains(tfs.InvariantFunctionNames, f => f == "func1");
            Assert.Contains(tfs.InvariantFunctionNames, f => f == "func2");
        }

        [Fact]
        [Obsolete("Contains testing Obsolete functions")]
        public void TextFunctionSet_TwoOverloads()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1");
            var func2 = new TestTexlFunction("func1", DType.Number);
            tfs.Add(func1);
            tfs.Add(func2);

            Assert.Empty(tfs.Enums);
            Assert.Equal(2, tfs.Functions.Count());
            Assert.Contains(tfs.Functions, f => f == func1);
            Assert.Contains(tfs.Functions, f => f == func2);
            Assert.Contains(tfs.WithName("func1"), f => f == func1);
            Assert.Contains(tfs.WithInvariantName("func1"), f => f == func1);
            Assert.Empty(tfs.WithName("FUNC1"));
            Assert.Contains(tfs.WithInvariantName("FUNC1"), f => f == func1);
            Assert.True(tfs.AnyWithName("func1"));
            Assert.False(tfs.AnyWithName("FUNC1"));
            Assert.Equal(2, tfs.Count());
            Assert.Single(tfs.FunctionNames);
            Assert.Single(tfs.InvariantFunctionNames);
            Assert.Contains(tfs.FunctionNames, f => f == "func1");
            Assert.Contains(tfs.InvariantFunctionNames, f => f == "func1");
        }

        [Fact]
        public void TextFunctionSet_WithEnums()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1", requiredEnums: 1);
            var func2 = new TestTexlFunction("func1", DType.Number, requiredEnums: 2);

            tfs.Add(func1);
            Assert.Single(tfs.Enums);

            tfs.Add(func2);
            Assert.Equal(3, tfs.Enums.Count());
            Assert.Equal(2, tfs.Enums.Distinct().Count());
        }

        [Fact]
        [Obsolete("Contains testing Obsolete functions")]
        public void TextFunctionSet_Remove()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1", requiredEnums: 1);

            tfs.Add(func1);
            tfs.RemoveAll("func1");

            Assert.Empty(tfs.Enums);
            Assert.Empty(tfs.Namespaces);
            Assert.Empty(tfs.Functions);
            Assert.Empty(tfs.FunctionNames);
            Assert.Empty(tfs.InvariantFunctionNames);
            Assert.Empty(tfs.WithName("func1"));
            Assert.Empty(tfs.WithInvariantName("func1"));
            Assert.Empty(tfs.WithNamespace(DPath.Root));

            tfs.Add(func1);
            tfs.RemoveAll(func1);

            Assert.Empty(tfs.Enums);
            Assert.Empty(tfs.Namespaces);
            Assert.Empty(tfs.Functions);
            Assert.Empty(tfs.FunctionNames);
            Assert.Empty(tfs.InvariantFunctionNames);
            Assert.Empty(tfs.WithName("func1"));
            Assert.Empty(tfs.WithInvariantName("func1"));
            Assert.Empty(tfs.WithNamespace(DPath.Root));
        }

        [Fact]
        [Obsolete("Contains testing Obsolete functions")]
        public void TextFunctionSet_Remove2()
        {
            var tfs = new TexlFunctionSet();
            var func1 = new TestTexlFunction("func1", requiredEnums: 1);
            var func2 = new TestTexlFunction("func1", DType.Number, requiredEnums: 3);

            tfs.Add(func1);
            tfs.Add(func2);

            Assert.Equal("Enum0, Enum0, Enum1, Enum2", string.Join(", ", tfs.Enums.OrderBy(x => x)));
            Assert.Equal(2, tfs.Functions.Count());
            Assert.Contains(tfs.Functions, f => f == func1);
            Assert.Contains(tfs.Functions, f => f == func2);           
            Assert.Single(tfs.FunctionNames);
            Assert.Single(tfs.InvariantFunctionNames);
            Assert.Contains(tfs.FunctionNames, f => f == "func1");
            Assert.Contains(tfs.InvariantFunctionNames, f => f == "func1");

            tfs.RemoveAll("func1");

            Assert.Empty(tfs.Enums);
            Assert.Empty(tfs.Namespaces);
            Assert.Empty(tfs.Functions);
            Assert.Empty(tfs.FunctionNames);
            Assert.Empty(tfs.InvariantFunctionNames);
            Assert.Empty(tfs.WithName("func1"));
            Assert.Empty(tfs.WithInvariantName("func1"));
            Assert.Empty(tfs.WithNamespace(DPath.Root));

            tfs.Add(func1);
            tfs.Add(func2);
            tfs.RemoveAll(func1);

            Assert.Equal("Enum0, Enum1, Enum2", string.Join(", ",  tfs.Enums.OrderBy(x => x)));
            Assert.Single(tfs.Namespaces); // DPath.Root
            Assert.Single(tfs.Functions);
            Assert.Single(tfs.FunctionNames);
            Assert.Single(tfs.InvariantFunctionNames);
            Assert.Single(tfs.WithName("func1"));
            Assert.Single(tfs.WithInvariantName("func1"));            
            Assert.Single(tfs.WithNamespace(DPath.Root));

            tfs.RemoveAll(func2);

            Assert.Empty(tfs.Enums);
            Assert.Empty(tfs.Namespaces);
            Assert.Empty(tfs.Functions);
            Assert.Empty(tfs.FunctionNames);
            Assert.Empty(tfs.InvariantFunctionNames);
            Assert.Empty(tfs.WithName("func1"));
            Assert.Empty(tfs.WithInvariantName("func1"));
            Assert.Empty(tfs.WithNamespace(DPath.Root));
        }


        private class TestTexlFunction : TexlFunction
        {
            private readonly int _requiredEnums = 0;
                
            public TestTexlFunction(string name, DType type = null, int requiredEnums = 0)
                : this(DPath.Root, name, name, (string locale) => name, FunctionCategories.Text, type ?? DType.String, 0, 1, 1, type ?? DType.String)
            {
                _requiredEnums = requiredEnums;
            }

            public TestTexlFunction(DPath theNamespace, string name, string localeSpecificName, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes) 
                : base(theNamespace, name, localeSpecificName, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<string> GetRequiredEnumNames()
            {
                return _requiredEnums == 0 
                    ? Enumerable.Empty<string>() 
                    : Enumerable.Range(0, _requiredEnums).Select(n => $"Enum{n}");
            }
        }
    }
}
