// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UDFHasSameDefinitionTests : PowerFxTest
    {
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

        [Theory]
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: Number): Number = Abs(x);", true)]
        
        // test with different udf body
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: Number): Number = Abs( /*Hello*/ x);", false)]
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: Number): Number = Abs(5);", false)]

        // test with different whitespace and trivials
        [InlineData("Foo(x: Number):   Number =   Abs(x)  ;", "Foo(x:Number):Number=Abs(x);", true)]
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: /*comment*/ Number): Number = /*comment*/ Abs(x);", true)]

        // test with different function names
        [InlineData("Foo(x: Number): Number = Abs(x);", "FooBar(x: Number): Number = Abs(x);", false)]

        // test with different parameter names
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(y: Number): Number = Abs(x);", false)]
        [InlineData("Foo(a: Number, b: Number, c: Number): Number = a+b+c;", "Foo(a: Number, b: Number, d: Number): Number = a+b+c;", false)]

        // test with type aliases
        [InlineData("Foo(a: Number, b: Number, c: Number): Number = a+b+c;", "Foo(a: Number, b: Number, c: Num): Number = a+b+c;", true)]

        // test with different parameter & return types
        [InlineData("Foo(x: Boolean): Number = Abs(x);", "Foo(x: Number): Number = Abs(x);", false)]
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: Number): Boolean = Abs(x);", false)]

        // test with different parameter order
        [InlineData("Foo(a: Number, b: Number, c: Number): Number = a+b+c;", "Foo(b: Number, c: Number, a: Number): Number = a+b+c;", false)]

        // Imperative UDF vs Declarative UDF
        [InlineData("Foo(x: Number): Number = Abs(x);", "Foo(x: Number): Number = {Abs(x)};", false)]
        public void TestSimpleUDFSameness(string udfFormula1, string udfFormula2, bool areSame)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true
            };

            var types = FormulaType.PrimitiveTypes.Union(new Dictionary<DName, FormulaType>() 
            {
                // Adds type aliases for testing
                { new DName("Num"), FormulaType.Number },
            });

            TestSameness(udfFormula1, udfFormula2, parserOptions, types, areSame);
        }
        
        [Theory]

        // test with associated data sources
        [InlineData("F(x: DS1): Number = First(x).a;", "F(x: DS1): Number = First(x).a;", true)]
        [InlineData("F(x: DS1): Number = First(x).a;", "F(x: DS2): Number = First(x).a;", false)]
        [InlineData("F(): DS1 = x;", "F(): DS1 = x;", true)]
        [InlineData("F(): DS1 = x;", "F(): DS2 = x;", false)]

        // test with complex aliases
        [InlineData("F(x: T1): Number = First(x).a;", "F(x: T2): Number = First(x).a;", true)]
        [InlineData("F(): T1 = x;", "F(): T2 = x;", true)]

        // test with deeply nested types
        [InlineData("F(x: CT1): CT1 = x;", "F(x: CT2): CT2 = x;", false)]
        [InlineData("F(x: CT1): CT1 = x;", "F(/*same*/ x: CT1): CT1 = x;", true)]
        public void TestComplexUDFSameness(string udfFormula1, string udfFormula2, bool areSame)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true
            };

            var schema = TestUtils.DT("*[a: n, b:s]");
            var ds1 = new TestDataSource("DS1", schema);
            var ds2 = new TestDataSource("DS2", schema);

            var complexType1 = TestUtils.DT("*[a: ![b: ![c: *[d: n, e:s]]]]");
            var complexType2 = TestUtils.DT("*[a: ![b: ![c: *[d: n, e:b]]]]");

            var types = FormulaType.PrimitiveTypes.Union(new Dictionary<DName, FormulaType>()
            {
                { new DName("DS1"), FormulaType.Build(ds1.Type) },
                { new DName("DS2"), FormulaType.Build(ds2.Type) },
                { new DName("T1"), FormulaType.Build(schema) },
                { new DName("T2"), FormulaType.Build(schema) },
                { new DName("CT1"), FormulaType.Build(complexType1) },
                { new DName("CT2"), FormulaType.Build(complexType2) },
            });

            TestSameness(udfFormula1, udfFormula2, parserOptions, types, areSame);
        }

        private void TestSameness(string udfFormula1, string udfFormula2, ParserOptions parserOptions, IEnumerable<KeyValuePair<DName, FormulaType>> types, bool areSame)
        {
            var udf1 = ParseAndCreateUDF(udfFormula1, parserOptions, types);
            var udf2 = ParseAndCreateUDF(udfFormula2, parserOptions, types);
            var udf1Body = udf1.UdfBody.GetCompleteSpan().GetFragment(udfFormula1);
            var udf2Body = udf2.UdfBody.GetCompleteSpan().GetFragment(udfFormula2);

            var result1 = udf1.HasSameDefintion(udfFormula1, udf2, udf2Body);
            var result2 = udf2.HasSameDefintion(udfFormula2, udf1, udf1Body);

            Assert.Equal(areSame, result1);
            Assert.Equal(areSame, result2);
        }

        private static UserDefinedFunction ParseAndCreateUDF(string script, ParserOptions parserOptions, IEnumerable<KeyValuePair<DName, FormulaType>> types)
        {
            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library, types);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), nameResolver, out var errors);
            
            // Ensure no errors
            Assert.Empty(errors);
            Assert.False(parseResult.HasErrors);

            return udfs.First();
        }
    }
}
