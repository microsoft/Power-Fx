// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UserDefinedTypeTests : PowerFxTest
    {
        [Theory]
        [InlineData("Point = Type({ x: Number, y: Number })", "![x:n,y:n]")]
        [InlineData("Points = Type([{ x: Number, y: Number }])", "*[x:n,y:n]")]
        [InlineData("Person = Type({ name: Text, age: Number })", "![name:s,age:n]")]
        [InlineData("People = Type([{ name: Text, age: Number }])", "*[name:s,age:n]")]

        public void TestUserDefinedType(string typeDefinition, string expected)
        {
            var parseOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true
            };

            var primitiveTypes = PrimitiveTypesSymbolTable.Instance;

            UserDefinitions.ProcessUserDefinitions(typeDefinition, parseOptions, out UserDefinitionResult results, extraSymbols: primitiveTypes);

            Assert.NotEmpty(results.DefinedTypes);

            var resultType = results.DefinedTypes.First().Value._type;
            Assert.Equal(TestUtils.DT(expected), resultType);
        }

        [Theory]
        [InlineData("Point = Type({ x: Number, y: Number })", 1)]
        [InlineData("Point = Type({ x: Number, y: Number }); Points = Type([Point])", 2)]
        [InlineData("Point = Type(5+5); Points = Type([Point]); People = Type([{ name: Text, age: Number }])", 1)]
        [InlineData("Point = Type({a:); Points = Type([Point]); People = Type([{ name: Text, age })", 0)]
        public void TestValidUDTCounts(string typeDefinition, int expected)
        {
            var parseOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true
            };

            var primitiveTypes = PrimitiveTypesSymbolTable.Instance;

            UserDefinitions.ProcessUserDefinitions(typeDefinition, parseOptions, out UserDefinitionResult results, extraSymbols: primitiveTypes);

            Assert.Equal(expected, results.DefinedTypes.Count());
        }
    }
}
