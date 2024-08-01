// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

// Todo: Refactor and unify common AsType. IsType and ParseJSON tests

namespace Microsoft.PowerFx.Json.Tests
{
    public class AsTypeUOTests
    {
        [Fact]
        public void PrimitivesTest()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
            var engine = new RecalcEngine(config);

            var parserOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true,
            };

            // custom-type type alias
            engine.AddUserDefinitions("T = Type(Number);");

            var t1 = engine.Eval("AsType(ParseJSON(\"42\"), Number)");
            var t2 = engine.Eval("AsType(ParseJSON(\"17.29\"), Number)");
            var t3 = engine.Eval("AsType(ParseJSON(\"\"\"HelloWorld\"\"\"), Text)");
            var t4 = engine.Eval("AsType(ParseJSON(\"\"\"2000-01-01\"\"\"), Date)");
            var t5 = engine.Eval("AsType(ParseJSON(\"\"\"2000-01-01T00:00:01.100Z\"\"\"), DateTime)");
            var t6 = engine.Eval("AsType(ParseJSON(\"true\"), Boolean)");
            var t7 = engine.Eval("AsType(ParseJSON(\"false\"), Boolean)");
            var t8 = engine.Eval("AsType(ParseJSON(\"1234.56789\"), Decimal)");
            var t9 = engine.Eval("AsType(ParseJSON(\"42\"), T)");
            var t10 = engine.Eval("AsType(ParseJSON(\"\"\"Power Fx\"\"\"), Type(Text))", options: parserOptions);

            Assert.Equal(42, ((NumberValue)t1).Value);
            Assert.Equal(17.29, ((NumberValue)t2).Value);
            Assert.Equal("HelloWorld", ((StringValue)t3).Value);
            Assert.Equal(new DateTime(2000, 1, 1), ((DateValue)t4).GetConvertedValue(TimeZoneInfo.Utc));
            Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 1, 100), ((DateTimeValue)t5).GetConvertedValue(TimeZoneInfo.Utc));
            Assert.True(((BooleanValue)t6).Value);
            Assert.False(((BooleanValue)t7).Value);
            Assert.Equal(1234.56789m, ((DecimalValue)t8).Value);
            Assert.Equal(42, ((NumberValue)t9).Value);
            Assert.Equal("Power Fx", ((StringValue)t10).Value);
        }

        [Fact]
        public void RecordsTest()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
            var engine = new RecalcEngine(config);

            var parserOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true,
            };

            engine.AddUserDefinitions("T = Type({a: Number});");

            var t1 = engine.Eval("AsType(ParseJSON(\"{\"\"a\"\": 5}\"), Type({a: Number}))", options: parserOptions);
            var t2 = engine.Eval("AsType(ParseJSON(\"{\"\"a\"\": 42}\"), T).a", options: parserOptions);
            var t3 = engine.Eval("AsType(ParseJSON(\"{\"\"a\"\": 5, \"\"b\"\": 6}\"), Type({a: Number}))", options: parserOptions);

            Assert.Equal(5, ((NumberValue)((RecordValue)t1).GetField("a")).Value);
            Assert.Equal(42, ((NumberValue)t2).Value);
            Assert.True(((ErrorValue)t3).Errors.Any());
        }

        [Fact]
        public void TablesTest()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
            var engine = new RecalcEngine(config);

            var parserOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true,
            };

            engine.AddUserDefinitions("T = Type([{a: Number}]);");

            var t1 = engine.Eval("First(AsType(ParseJSON(\"[{\"\"a\"\": 5}]\"), Type([{a: Number}])))", options: parserOptions);
            var t2 = engine.Eval("First(AsType(ParseJSON(\"{\"\"a\"\": 42}\"), T)).a", options: parserOptions);
            var t3 = engine.Eval("AsType(ParseJSON(\"[{\"\"a\"\": 5, \"\"b\"\": 6}]\"), Type([{a: Number}]))", options: parserOptions);

            Assert.Equal(5, ((NumberValue)((RecordValue)t1).GetField("a")).Value);
            Assert.Equal(42, ((NumberValue)t2).Value);
            Assert.True(((ErrorValue)t3).Errors.Any());
        }
    }
}
