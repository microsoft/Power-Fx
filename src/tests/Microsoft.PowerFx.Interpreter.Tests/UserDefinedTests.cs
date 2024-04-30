// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserDefinedTests
    {
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

        [Theory]
        [InlineData("x=1;y=2;z=x+y;", "Float(Abs(-(x+y+z)))", 6d)]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", 2d)]
        public void NamedFormulaEntryTest(string script, string expression, double expected)
        {
            var engine = new RecalcEngine();

            engine.AddUserDefinitions(script);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (NumberValue)check.GetEvaluator().Eval();
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) }; num = 3;")]
        public void ValidUDFBodyTest(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.False(errors.Any());
        }

        [Theory]
        [InlineData("F1(x:Number) : Number = { Set(a, x); x+1; };")]
        [InlineData("F1(x:Number) : Boolean = { Set(a, x); Today(); };")]
        public void ValidUDFBodyTest2(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.False(errors.Any());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b);")]
        public void InvalidUDFBodyTest(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.True(errors.Any());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;}; num = 3;", 1, 1)]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;};;;;;;;; num = 3;", 1, 1)]
        public void InvalidUDFBodyTest2(string script, int udfCount, int nfCount)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.True(errors.Any());
            Assert.Equal(udfCount, udfs.Count());
            Assert.Equal(nfCount, parseResult.NamedFormulas.Count());
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        public void ImperativeUdfsEnabledOrDisabled(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = false,
                Culture = CultureInfo.InvariantCulture
            };

            var parseResult = UserDefinitions.Parse(script, options);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.True(errors.Any());

            options = new ParserOptions()
            {
                AllowsSideEffects = true,
                Culture = CultureInfo.InvariantCulture
            };
            parseResult = UserDefinitions.Parse(script, options);
            udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.False(errors.Any());
        }
    }
}
