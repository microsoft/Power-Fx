// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UserDefinedTypeTests : PowerFxTest
    {
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

        [Theory]

        // check record, table types with primitive types
        [InlineData("Point = Type({ x: Number, y: Number })", "![x:n,y:n]", true)]
        [InlineData("Points = Type([{ x: Number, y: Number }])", "*[x:n,y:n]", true)]
        [InlineData("Person = Type({ name: Text, dob: Date })", "![name:s,dob:D]", true)]
        [InlineData("People = Type([{ name: Text, isReady: Boolean }])", "*[name:s,isReady:b]", true)]
        [InlineData("Heights = Type([Number])", "*[Value:n]", true)]
        [InlineData("Palette = Type([Color])", "*[Value:c]", true)]
        
        // type alias
        [InlineData("DTNZ = Type(DateTimeTZInd)", "Z", true)]

        // nested record types
        [InlineData("Nested = Type({a: {b: DateTime, c: {d: GUID, e: Hyperlink}}, x: Time})", "![a:![b:d, c:![d:g, e:h]], x:T]", true)]

        // invalid types
        [InlineData("Pics = Type([Image])", "*[Value:i]", false)]
        public void TestUserDefinedType(string typeDefinition, string expected, bool isValid)
        {
            var parseOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true
            };

            UserDefinitions.ProcessUserDefinitions(typeDefinition, parseOptions, out UserDefinitionResult results, nameResolver: _primitiveTypes);

            if (isValid) 
            {
                Assert.NotEmpty(results.DefinedTypes);
                var resultType = results.DefinedTypes.First().Type._type;
                Assert.Equal(TestUtils.DT(expected), resultType);
            }
            else
            {
                Assert.Empty(results.DefinedTypes);
                Assert.NotEmpty(results.Errors);
            }
        }

        [Theory]
        [InlineData("X = 5; Point = Type({ x: Number, y: Number })", 1)]
        [InlineData("Point = Type({ x: Number, y: Number }); Points = Type([Point])", 2)]

        // mix named formula with named type
        [InlineData("X = 5; Point = Type({ x: X, y: Number }); Points = Type([Point])", 0)]

        // have invalid type expression
        [InlineData("WrongType = Type(5+5); WrongTypes = Type([WrongType]); People = Type([{ name: Text, age: Number }])", 1)]

        // have incomplete expressions and parse errors
        [InlineData("Point = Type({a:); Points = Type([Point]); People = Type([{ name: Text, age })", 0)]
        [InlineData("Point = Type({a:; Points = Type([Point]); People = Type([{ name: Text, age: Number })", 1)]

        // redeclare type
        [InlineData("Point = Type({ x: Number, y: Number }); Point = Type(Number);", 1)]

        // cyclic definition
        [InlineData("B = Type({ x: A }); A = Type(B);", 0)]

        // complex resolutions
        [InlineData("C = Type({x: Boolean, y: Date, f: B});B = Type({ x: A }); A = Type(Number);", 3)]
        [InlineData("D = Type({nArray: [Number]}), C = Type({x: Boolean, y: Date, f: B});B = Type({ x: A }); A = Type([C]);", 1)]

        // with Invalid types
        [InlineData("A = Type(Blob); B = Type({x: Currency}); C = Type([DateTime]); D = Type(None)", 2)]

        // have named formulas and udf in the script
        [InlineData("NAlias = Type(Number);X = 5, ADDX(n:Number): Number = n + X; SomeType = Type(UntypedObject)", 2)]
        public void TestValidUDTCounts(string typeDefinition, int expected)
        {
            var parseOptions = new ParserOptions
            {
                AllowParseAsTypeLiteral = true
            };

            UserDefinitions.ProcessUserDefinitions(typeDefinition, parseOptions, out UserDefinitionResult results, nameResolver: _primitiveTypes);

            Assert.Equal(expected, results.DefinedTypes.Count());
        }
    }
}
