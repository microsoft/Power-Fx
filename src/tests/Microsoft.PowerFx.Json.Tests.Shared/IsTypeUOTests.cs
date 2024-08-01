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
    public class IsTypeUOTests
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

            var t1 = engine.Eval("IsType(ParseJSON(\"42\"), Number)");
            var t2 = engine.Eval("IsType(ParseJSON(\"17.29\"), Number)");
            var t3 = engine.Eval("IsType(ParseJSON(\"\"\"HelloWorld\"\"\"), Text)");
            var t4 = engine.Eval("IsType(ParseJSON(\"\"\"2000-01-01\"\"\"), Date)");
            var t5 = engine.Eval("IsType(ParseJSON(\"\"\"2000-01-01T00:00:01.100Z\"\"\"), DateTime)");
            var t6 = engine.Eval("IsType(ParseJSON(\"true\"), Boolean)");
            var t7 = engine.Eval("IsType(ParseJSON(\"false\"), Boolean)");
            var t8 = engine.Eval("IsType(ParseJSON(\"1234.56789\"), Decimal)");
            var t9 = engine.Eval("IsType(ParseJSON(\"42\"), T)");
            var t10 = engine.Eval("IsType(ParseJSON(\"\"\"Power Fx\"\"\"), Type(Text))", options: parserOptions);

            Assert.True(((BooleanValue)t1).Value);
            Assert.True(((BooleanValue)t2).Value);
            Assert.True(((BooleanValue)t3).Value);
            Assert.True(((BooleanValue)t4).Value);
            Assert.True(((BooleanValue)t5).Value);
            Assert.True(((BooleanValue)t6).Value);
            Assert.True(((BooleanValue)t7).Value);
            Assert.True(((BooleanValue)t8).Value);
            Assert.True(((BooleanValue)t9).Value);
            Assert.True(((BooleanValue)t10).Value);
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

            var t1 = engine.Eval("IsType(ParseJSON(\"{\"\"a\"\": 5}\"), Type({a: Number}))", options: parserOptions);
            var t2 = engine.Eval("IsType(ParseJSON(\"{\"\"a\"\": 42}\"), T)", options: parserOptions);
            var t3 = engine.Eval("IsType(ParseJSON(\"{\"\"a\"\": 5, \"\"b\"\": 6}\"), Type({a: Number}))", options: parserOptions);

            Assert.True(((BooleanValue)t1).Value);
            Assert.True(((BooleanValue)t2).Value);
            Assert.False(((BooleanValue)t3).Value);
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

            var t1 = engine.Eval("IsType(ParseJSON(\"[{\"\"a\"\": 5}]\"), Type([{a: Number}]))", options: parserOptions);
            var t2 = engine.Eval("IsType(ParseJSON(\"{\"\"a\"\": 42}\"), T)", options: parserOptions);
            var t3 = engine.Eval("IsType(ParseJSON(\"[{\"\"a\"\": 5, \"\"b\"\": 6}]\"), Type([{a: Number}]))", options: parserOptions);

            Assert.True(((BooleanValue)t1).Value);
            Assert.True(((BooleanValue)t2).Value);
            Assert.False(((BooleanValue)t3).Value);
        }
    }
}
