﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class AsTypeIsTypeParseJSONTests
    {
        private RecalcEngine SetupEngine(bool udtFeaturedEnabled = true)
        {
            var features = new Features(Features.PowerFxV1)
            {
                IsUserDefinedTypesEnabled = udtFeaturedEnabled,
            };
            var config = new PowerFxConfig(features);
            config.EnableJsonFunctions();
            return new RecalcEngine(config);
        }

        [Fact]
        public void PrimitivesTest()
        {
            var engine = SetupEngine();

            // custom-type type alias
            engine.AddUserDefinitions("T := Type(Number);");

            // Positive tests
            CheckIsTypeAsTypeParseJSON(engine, "\"42\"", "Number", 42D);
            CheckIsTypeAsTypeParseJSON(engine, "\"17.29\"", "Number", 17.29D);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"HelloWorld\"\"\"", "Text", "HelloWorld");
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"2000-01-01\"\"\"", "Date", new DateTime(2000, 1, 1));
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"2000-01-01T00:00:01.100\"\"\"", "DateTime", new DateTime(2000, 1, 1, 0, 0, 1, 100));
            CheckIsTypeAsTypeParseJSON(engine, "\"true\"", "Boolean", true);
            CheckIsTypeAsTypeParseJSON(engine, "\"false\"", "Boolean", false);
            CheckIsTypeAsTypeParseJSON(engine, "\"1234.56789\"", "Decimal", 1234.56789m);
            CheckIsTypeAsTypeParseJSON(engine, "\"42\"", "T", 42D);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"Power Fx\"\"\"", "Type(Text)", "Power Fx");
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"2000-01-01T00:00:01.100Z\"\"\"", "DateTimeTZInd", new DateTime(2000, 1, 1, 0, 0, 1, 100));
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"11:59:59\"\"\"", "Time", new TimeSpan(11, 59, 59));

            // Negative tests - Coercions not allowed
            CheckIsTypeAsTypeParseJSON(engine, "\"42\"", "Text", string.Empty, false);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"42\"\"\"", "Number", string.Empty, false);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"42\"\"\"", "Decimal", string.Empty, false);
            CheckIsTypeAsTypeParseJSON(engine, "\"0\"", "Boolean", false, false);
            CheckIsTypeAsTypeParseJSON(engine, "\"true\"", "Number", false, false);

            // Negative tests - types not supported in FromJSON converter
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"42\"", "None", TexlStrings.ErrUnsupportedTypeInTypeArgument.Key);
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"\"\"RED\"\"\"", "Color", TexlStrings.ErrUnsupportedTypeInTypeArgument.Key);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"abcd-efgh-1234-ijkl\"\"\"", "GUID", string.Empty, false);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"foo/bar/uri\"\"\"", "Hyperlink", string.Empty, false);
        }

        [Fact]
        public void RecordsTest()
        {
            var engine = SetupEngine();

            engine.AddUserDefinitions("T := Type({a: Number});");

            dynamic obj1 = new ExpandoObject();
            obj1.a = 5D;

            dynamic obj2 = new ExpandoObject();
            obj2.a = new ExpandoObject();
            obj2.a.b = new ExpandoObject();
            obj2.a.b.c = false;

            dynamic obj3 = new ExpandoObject();
            obj3.a = new object[] { 1m, 2m, 3m, 4m };

            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "T", obj1);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "Type({a: Number})", obj1);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": { \"\"b\"\": {\"\"c\"\":  false}}}\"", "Type({a: {b: {c: Boolean }}})", obj2);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": [1, 2, 3, 4]}\"", "Type({a: [Decimal]})", obj3);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5, \"\"b\"\": 6}\"", "Type({a: Number})", obj1);

            // Negative Tests
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "Type({a: Text})", obj1, isValid: false);
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"{\"\"a\"\": \"\"foo/bar/uri\"\"}\"", "Type({a: Void})", TexlStrings.ErrTypeFunction_InvalidTypeExpression.Key);
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"{\"\"a\"\": \"\"foo/bar/uri\"\"}\"", "Type({a: Color})", TexlStrings.ErrUnsupportedTypeInTypeArgument.Key);
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"{\"\"a\"\": \"\"foo/bar/uri\"\"}\"", "Type(RecordOf(T))", TexlStrings.ErrTypeFunction_InvalidTypeExpression.Key);
        }

        [Fact]
        public void TablesTest()
        {
            var engine = SetupEngine();

            engine.AddUserDefinitions("T := Type([{a: Number}]);");

            var t1 = new object[] { 5D };
            var t2 = new object[] { 1m, 2m, 3m, 4m };
            var t3a = new object[] { true, true, false, true };
            var t3 = new object[] { t3a };

            dynamic obj1 = new ExpandoObject();
            obj1.a = 5D;

            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5}]\"", "T", t1);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5}]\"", "Type([{a: Number}])", t1);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5}]\"", "Type([RecordOf(T)])", t1);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "Type(RecordOf(T))", obj1);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": [true, true, false, true]}]\"", "Type([{a: [Boolean]}])", t3);
            CheckIsTypeAsTypeParseJSON(engine, "\"[1, 2, 3, 4]\"", "Type([Decimal])", t2);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5, \"\"b\"\": 6}]\"", "Type([{a: Number}])", t1);

            // Negative tests
            CheckIsTypeAsTypeParseJSON(engine, "\"[1, 2, 3, 4]\"", "Type([Text])", t2, false);
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, "\"[\"\"foo/bar/uri\"\"]\"", "Type([Color])", TexlStrings.ErrUnsupportedTypeInTypeArgument.Key);
        }

        [Theory]
        [InlineData("\"42\"", "SomeType", "ErrInvalidName")]
        [InlineData("\"42\"", "Type(5)", "ErrTypeFunction_InvalidTypeExpression")]
        [InlineData("\"42\"", "Text(42)", "ErrInvalidArgumentExpectedType")]
        [InlineData("\"\"\"Hello\"\"\"", "\"Hello\"", "ErrInvalidArgumentExpectedType")]
        [InlineData("\"{}\"", "Type([{a: 42}])", "ErrTypeFunction_InvalidTypeExpression")]
        public void TestCompileErrorsAllStronglyTypedOverloads(string expression, string type, string expectedError)
        {
            var engine = SetupEngine();
            CheckIsTypeAsTypeParseJSONCompileErrors(engine, expression, type, expectedError);
        }

        [Theory]
        [InlineData("AsType(ParseJSON(\"42\"))", "ErrBadArity")]
        [InlineData("IsType(ParseJSON(\"42\"))", "ErrBadArity")]
        [InlineData("AsType(ParseJSON(\"42\"), Number , Text(5))", "ErrBadArity")]
        [InlineData("IsType(ParseJSON(\"42\"), Number, 5)", "ErrBadArity")]
        [InlineData("AsType(ParseJSON(\"123\"), 1)", "ErrInvalidArgumentExpectedType")]
        [InlineData("AsType(Type(UntypedObject), ParseJSON(\"123\"))", "ErrTypeFunction_UnsupportedUsage")]
        [InlineData("IsType(Type(UntypedObject), Type(Boolean))", "ErrTypeFunction_UnsupportedUsage")]
        [InlineData("ParseJSON(Type(Text), Type(Text))", "ErrTypeFunction_UnsupportedUsage")]
        public void TestCompileErrors(string expression, string expectedError)
        {
            var engine = SetupEngine();

            var result = engine.Check(expression);
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == expectedError);
        }

        [Theory]
        [InlineData("AsType(ParseJSON(\"123\"), Number)")]
        [InlineData("IsType(ParseJSON(\"123\"), Type(Number))")]
        [InlineData("ParseJSON(\"\"\"Hello\"\"\", Type(Text))")]
        public void TestCompileErrorsWithUDTFeatureDisabled(string expression)
        {
            var engine = SetupEngine(udtFeaturedEnabled: false);
            var result = engine.Check(expression);
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == "ErrUserDefinedTypesDisabled");
        }

        [Fact]
        public void TestFunctionsWithTypeArgs()
        {
            var expectedFunctions = new HashSet<string> { "AsType", "IsType", "ParseJSON" };
            var functionWithTypeArgs = BuiltinFunctionsCore.TestOnly_AllBuiltinFunctions.Where(f => f.HasTypeArgs);

            Assert.All(functionWithTypeArgs, f => Assert.Contains(f.Name, expectedFunctions));
            Assert.All(functionWithTypeArgs, f => Assert.Contains(Enumerable.Range(0, f.MaxArity), i => f.ArgIsType(i)));

            var functionsWithoutTypeArgs = BuiltinFunctionsCore.TestOnly_AllBuiltinFunctions.Where(f => !f.HasTypeArgs);
            Assert.All(functionsWithoutTypeArgs, f => Assert.DoesNotContain(Enumerable.Range(0, Math.Min(f.MaxArity, 5)), i => f.ArgIsType(i)));
        }

        private void CheckIsTypeAsTypeParseJSON(RecalcEngine engine, string json, string type, object expectedValue, bool isValid = true, ParserOptions options = null)
        {
            var result = engine.Eval($"AsType(ParseJSON({json}), {type})", options: options);
            CheckResult(expectedValue, result, isValid);

            result = engine.Eval($"ParseJSON({json}, {type})", options: options);
            CheckResult(expectedValue, result, isValid);

            result = engine.Eval($"IsType(ParseJSON({json}), {type})", options: options);
            Assert.Equal(isValid, result.ToObject());
        }

        private void CheckResult(object expectedValue, FormulaValue resultValue, bool isValid)
        {
            if (isValid)
            {
                Assert.Equal(expectedValue, resultValue.ToObject());
            }
            else
            {
                Assert.True(resultValue is ErrorValue);
            }
        }

        private void CheckIsTypeAsTypeParseJSONCompileErrors(RecalcEngine engine, string json, string type, string expectedError, ParserOptions options = null)
        {
            var result = engine.Check($"AsType(ParseJSON({json}), {type})", options: options);
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == expectedError);

            result = engine.Check($"ParseJSON({json}, {type})", options: options);
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == expectedError);

            result = engine.Check($"IsType(ParseJSON({json}), {type})", options: options);
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == expectedError);
        }
    }
}
