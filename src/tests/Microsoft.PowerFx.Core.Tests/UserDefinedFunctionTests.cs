// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UserDefinedFunctionTests : PowerFxTest
    {
        private bool ProcessUserDefinitions(string script, out UserDefinitionResult userDefinitionResult)
        {
            return UserDefinitions.ProcessUserDefinitions(script, ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library), new Glue2DocumentBinderGlue(), BindingConfig.Default, out userDefinitionResult);
        }

        [Theory]
        [InlineData("Foo(x: Number): Number = Abs(x);")]
        [InlineData("Foo(x: Number): Number{ Abs(x); };")]
        public void TestUDFDeclaration(string script)
        {
            var userDefinitions = ProcessUserDefinitions(script, out var userDefinitionResult);
            var udf = userDefinitionResult.UDFs.FirstOrDefault();

            Assert.NotNull(udf);

            Assert.Empty(userDefinitionResult.NamedFormulas);
            Assert.Equal("Foo", udf.Name);
            Assert.True(udf.ReturnType.IsPrimitive);
            Assert.Empty(userDefinitionResult.Errors);
        }

        [Theory]
        [InlineData("Foo(x: Number): Number = Abs(x);", 1, 0, false)]
        [InlineData("Foo(x: Number): Number = Abs(x); x = 1;", 1, 1, false)]
        [InlineData("x = 1; Foo(x: Number): Number = Abs(x);", 1, 1, false)]
        [InlineData("/*this is a test*/ x = 1; Foo(x: Number): Number = Abs(x);", 1, 1, false)]
        [InlineData("x = 1; Foo(x: Number): Number = Abs(x); y = 2;", 1, 2, false)]
        [InlineData("Add(x: Number, y:Number): Number = x + y; Foo(x: Number): Number = Abs(x); y = 2;", 2, 1, false)]
        [InlineData("Foo(x: Number): Number = /*this is a test*/ Abs(x); y = 2;", 1, 1, false)]
        [InlineData("Add(x: Number, y:Number): Number = b + b; Foo(x: Number): Number = Abs(x); y = 2;", 2, 1, true)]
        [InlineData("Add(x: Number, y:Number): Boolean = x + y;", 1, 0, true)]
        [InlineData("Add(x: Number, y:Number): SomeType = x + y;", 0, 0, true)]
        [InlineData("Add(x: SomeType, y:Number): Number = x + y;", 0, 0, true)]
        [InlineData("Add(x: Number, y:Number): Number = x + y", 0, 0, true)]
        [InlineData("x = 1; Add(x: Number, y:Number): Number = x + y", 0, 1, true)]
        [InlineData("Add(x: Number, y:Number) = x + y;", 0, 0, true)]
        [InlineData("Add(x): Number = x + 2;", 0, 0, true)]
        [InlineData("Add(a:Number, b:Number): Number { a + b + 1; \n a + b; };", 1, 0, false)]
        [InlineData("Add(a:Number, b:Number): Number { a + b; };", 1, 0, false)]
        [InlineData("Add(a:Number, b:Number): Number { /*this is a test*/ a + b; };", 1, 0, false)]
        [InlineData("Add(a:Number, b:Number): Number { /*this is a test*/ a + b; ;", 0, 0, true)]
        [InlineData("Add(a:Number, a:Number): Number { a; };", 0, 0, true)]
        [InlineData(@"F2(b: Number): Number  = F1(b*3); F1(a:Number): Number = a*2;", 2, 0, false)]
        public void TestUDFNamedFormulaCounts(string script, int udfCount, int namedFormulaCount, bool expectErrors)
        {
            var userDefinitions = UserDefinitions.ProcessUserDefinitions(script, ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library), new Glue2DocumentBinderGlue(), BindingConfig.Default, out var userDefinitionResult);

            Assert.Equal(udfCount, userDefinitionResult.UDFs.Count());
            Assert.Equal(namedFormulaCount, userDefinitionResult.NamedFormulas.Count());
            Assert.Equal(expectErrors, userDefinitionResult.Errors.Any());
        }
    }
}
