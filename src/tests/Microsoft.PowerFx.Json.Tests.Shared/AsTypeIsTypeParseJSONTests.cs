// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class AsTypeIsTypeParseJSONTests
    {
        private static readonly ParserOptions ParseType = new ParserOptions
        {
            AllowParseAsTypeLiteral = true,
        };

        private RecalcEngine SetupEngine()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
            return new RecalcEngine(config);
        }

        [Fact]
        public void PrimitivesTest()
        {
            var engine = SetupEngine();

            // custom-type type alias
            engine.AddUserDefinitions("T = Type(Number);");

            CheckIsTypeAsTypeParseJSON(engine, "\"42\"", "Number", 42D);
            CheckIsTypeAsTypeParseJSON(engine, "\"17.29\"", "Number", 17.29D);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"HelloWorld\"\"\"", "Text", "HelloWorld");
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"2000-01-01\"\"\"", "Date", new DateTime(2000, 1, 1));
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"2000-01-01T00:00:01.100Z\"\"\"", "DateTime", new DateTime(2000, 1, 1, 0, 0, 1, 100));
            CheckIsTypeAsTypeParseJSON(engine, "\"true\"", "Boolean", true);
            CheckIsTypeAsTypeParseJSON(engine, "\"false\"", "Boolean", false);
            CheckIsTypeAsTypeParseJSON(engine, "\"1234.56789\"", "Decimal", 1234.56789m);
            CheckIsTypeAsTypeParseJSON(engine, "\"42\"", "T", 42D);
            CheckIsTypeAsTypeParseJSON(engine, "\"\"\"Power Fx\"\"\"", "Type(Text)", "Power Fx", options: ParseType);
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

        [Fact]
        public void RecordsTest()
        {
            var engine = SetupEngine();

            engine.AddUserDefinitions("T = Type({a: Number});");

            dynamic obj1 = new ExpandoObject();
            obj1.a = 5D;

            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "T", obj1);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5}\"", "Type({a: Number})", obj1, options: ParseType);
            CheckIsTypeAsTypeParseJSON(engine, "\"{\"\"a\"\": 5, \"\"b\"\": 6}\"", "Type({a: Number})", obj1, false, options: ParseType);
        }

        [Fact]
        public void TablesTest()
        {
            var engine = SetupEngine();

            engine.AddUserDefinitions("T = Type([{a: Number}]);");

            var t1 = new object[] { 5D };

            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5}]\"", "T", t1);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5}]\"", "Type([{a: Number}])", t1, options: ParseType);
            CheckIsTypeAsTypeParseJSON(engine, "\"[{\"\"a\"\": 5, \"\"b\"\": 6}]\"", "Type([{a: Number}])", t1, false, options: ParseType);
        }
    }
}
