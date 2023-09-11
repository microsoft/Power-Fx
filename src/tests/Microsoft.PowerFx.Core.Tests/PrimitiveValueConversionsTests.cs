// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PrimitiveValueConversionsTests
    {
        // Test how .net types marshal to FormulaType
        [Theory]
        [InlineData(typeof(double), typeof(NumberType))]
        [InlineData(typeof(int), typeof(DecimalType))]
        [InlineData(typeof(decimal), typeof(DecimalType))]
        [InlineData(typeof(long), typeof(DecimalType))]
        [InlineData(typeof(float), typeof(NumberType))]
        [InlineData(typeof(Guid), typeof(GuidType))]
        [InlineData(typeof(bool), typeof(BooleanType))]
        [InlineData(typeof(DateTime), typeof(DateTimeType))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeType))]
        [InlineData(typeof(TimeSpan), typeof(TimeType))]
        [InlineData(typeof(string), typeof(StringType))]
        [InlineData(typeof(object), null)] // no match
        public void TestAll(Type dotnetType, Type fxType)
        {
            var result = PrimitiveValueConversions.TryGetFormulaType(dotnetType, out var actualFxType);

            if (fxType == null)
            {
                Assert.False(result);
            }
            else
            {
                Assert.True(result);
                Assert.True(actualFxType.GetType() == fxType);

                var value = GetValue(dotnetType);
                var fxValue = PrimitiveValueConversions.Marshal(value, dotnetType);
                Assert.Equal(fxType, fxValue.Type.GetType());

                var expr = actualFxType.DefaultExpressionValue();

                var engine = GetEngineWithFeatureGatedFunctions();
                var options = new ParserOptions()
                {
                    NumberIsFloat = !(dotnetType == typeof(decimal) || dotnetType == typeof(long))
                };

                var check = engine.Check(expr, options);
                Assert.Equal(check.ReturnType, actualFxType);
            }
        }

        // Get an instance of the object we can test against the marhsaller. 
        private static object GetValue(Type t)
        {
            if (t == typeof(string))
            {
                return string.Empty;
            }

            if (t == typeof(Guid))
            {
                return Guid.Empty;
            }

            return Activator.CreateInstance(t);
        }
        
        // .Net dates map to FormulaType.DateTime, so need to explicitly test FormulaType.Date
        [Fact]
        public void TestDateOnly()
        {
            // Has non-zero time, 
            var d = new DateTime(2011, 3, 6);
            var t = new TimeSpan(5, 10, 23);
            var dt = d + t;
            var dto = new DateTimeOffset(dt);

            // Can't represent times as Dates. 
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(dt, FormulaType.Date));
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(dto, FormulaType.Date));

            Assert.Throws<ArgumentException>(() => FormulaValue.NewDateOnly(dt));

            var result1 = (DateTimeValue)PrimitiveValueConversions.Marshal(dt, FormulaType.DateTime);
            var result2 = FormulaValue.New(dt);
            AssertEqual(dt, result2);
            AssertEqual(result1, result2);

            var result3 = (DateTimeValue)PrimitiveValueConversions.Marshal(dto, FormulaType.DateTime);
            AssertEqual(result1, result3);

            // Success
            var result4 = FormulaValue.NewDateOnly(d);
            AssertEqual(d, result4);
            var result5 = (DateValue)PrimitiveValueConversions.Marshal(d, FormulaType.Date);
            AssertEqual(result4, result5);
        }

        [Fact]
        public void TestBlank()
        {
            var x1 = FormulaValue.NewBlank();
            Assert.Equal(FormulaType.Blank, x1.Type);
            Assert.IsType<BlankValue>(x1);

            var x2 = FormulaValue.NewBlank(null);
            Assert.Equal(FormulaType.Blank, x2.Type);
            Assert.IsType<BlankValue>(x2);

            var t = FormulaType.String;
            var x3 = FormulaValue.NewBlank(t);
            Assert.Equal(t, x3.Type);
            Assert.IsType<BlankValue>(x3);

            var value = PrimitiveValueConversions.Marshal(null, FormulaType.Number);
            Assert.IsType<BlankValue>(value);
            Assert.IsType<NumberType>(value.Type);

            var value2 = PrimitiveValueConversions.Marshal(null, typeof(int));
            Assert.IsType<BlankValue>(value2);
            Assert.IsType<DecimalType>(value2.Type);
        }

        [Theory]
        [InlineData((ushort)123)] // Uint16
        [InlineData((short)123)] // int16
        [InlineData(123U)] // Uint32
        [InlineData(123)] // int32
        [InlineData(123UL)] // Uint64
        [InlineData(123L)] // int64
        [InlineData(123f)] // Single
        [InlineData(123d)] // Double        
        public void TryMarshalNumberTest(object value)
        {
            // To double 
            var ok = PrimitiveValueConversions.TryMarshal(value, FormulaType.Number, out var result);
            Assert.True(ok);

            var expected = Convert.ToDouble(value);
            Assert.Equal(expected, result.ToObject());
            Assert.IsType<NumberValue>(result);
            Assert.Equal(FormulaType.Number, result.Type);

            // To decimal 
            ok = PrimitiveValueConversions.TryMarshal(value, FormulaType.Decimal, out result);
            Assert.True(ok);

            var expected2 = Convert.ToDecimal(value);
            Assert.Equal(expected2, result.ToObject());
            Assert.IsType<DecimalValue>(result);
            Assert.Equal(FormulaType.Decimal, result.Type);
        }

        [Fact]
        public void TryMarshalOverflowNumberTest()
        {
            object value = double.MaxValue;
            var ok = PrimitiveValueConversions.TryMarshal(value, FormulaType.Decimal, out var result);
            Assert.True(ok);

            Assert.IsType<ErrorValue>(result);
            var error = ((ErrorValue)result).Errors.First();
            Assert.Equal(ErrorKind.Numeric, error.Kind);
            Assert.Equal(FormulaType.Decimal, result.Type);
        }

        [Theory]
        [InlineData("5")] // string, not parsing. 
        [InlineData(true)]        
        public void TryMarshalFailNumberTest(object value)
        {
            // Not a parse. 
            // Also more strict than just Fx coercion. 
            var ok = PrimitiveValueConversions.TryMarshal(value, FormulaType.Number, out var result);
            Assert.False(ok);
            Assert.Null(result);

            ok = PrimitiveValueConversions.TryMarshal(value, FormulaType.Decimal, out result);
            Assert.False(ok);
            Assert.Null(result);
        }

        [Fact]
        public void MarshalFailures()
        {
            // Mismatch value and type
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(3, typeof(string)));

            // Can't use primitive marshaller on complex types. 
            var complex = new { x = 15 };
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal(complex, complex.GetType()));

            var fxType = RecordType.Empty();
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal(complex, fxType));

            // Verify illegal combinations fail.
            Assert.Throws<InvalidCastException>(() => PrimitiveValueConversions.Marshal(3, FormulaType.String));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.Number));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.Date));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.DateTime));
        }

        internal class TestFormulaType : FormulaType
        {
            public TestFormulaType(DType type)
                : base(type)
            {
            }

            public override void Visit(ITypeVisitor vistor)
            {
                throw new NotImplementedException();
            }
        }

        [Theory]
        [InlineData("s", "b", "UnaryOpKind.TextToBoolean")]
        [InlineData("s", "g", "UnaryOpKind.TextToGUID")] // new
        [InlineData("s", "i", "UnaryOpKind.TextToImage")] // Existing, not implemented
        [InlineData("s", "m", "UnaryOpKind.TextToMedia")] // Existing, not implemented
        [InlineData("s", "o", "UnaryOpKind.TextToBlob")] // Existing, not implemented
        [InlineData("s", "h", "UnaryOpKind.TextToHyperlink")] // Existing, not implemented
        [InlineData("s", "n", "function:Float")]
        [InlineData("s", "d", "function:DateTimeValue")]
        [InlineData("s", "D", "function:DateValue")]
        [InlineData("s", "T", "function:TimeValue")]
        [InlineData("n", "s", "UnaryOpKind.NumberToText")]
        [InlineData("D", "s", "function:Text")]
        [InlineData("d", "s", "function:Text")]
        [InlineData("T", "s", "function:Text")]
        [InlineData("b", "s", "UnaryOpKind.BooleanToText")]
        [InlineData("g", "s", "UnaryOpKind.GUIDToText")] // new
        [InlineData("o", "s", "UnaryOpKind.BlobToText")] // new
        [InlineData("m", "s", "UnaryOpKind.MediaToText")] // new
        [InlineData("i", "s", "UnaryOpKind.ImageToText")] // Existing, not implemented
        [InlineData("p", "s", "UnaryOpKind.PenImageToText")] // new
        [InlineData("m", "h", "UnaryOpKind.MediaToHyperlink")] // Existing, not implemented
        [InlineData("i", "h", "UnaryOpKind.ImageToHyperlink")] // Existing, not implemented
        [InlineData("o", "h", "UnaryOpKind.BlobToHyperlink")] // Existing, not implemented
        [InlineData("p", "h", "UnaryOpKind.PenImageToHyperlink")] // new
        [InlineData("h", "i", "UnaryOpKind.HyperlinkToImage")] // new
        [InlineData("o", "i", "UnaryOpKind.BlobToImage")] // new
        [InlineData("p", "i", "UnaryOpKind.PenImageToImage")] // new
        [InlineData("h", "m", "UnaryOpKind.HyperlinkToMedia")] // new
        [InlineData("o", "m", "UnaryOpKind.BlobToMedia")] // new
        [InlineData("h", "o", "UnaryOpKind.HyperlinkToBlob")] // new
        [InlineData("d", "D", "UnaryOpKind.DateTimeToDate")] // new
        [InlineData("d", "T", "UnaryOpKind.DateTimeToTime")] // new
        [InlineData("T", "D", "UnaryOpKind.TimeToDate")] // new
        [InlineData("T", "d", "UnaryOpKind.TimeToDateTime")] // new
        [InlineData("D", "T", "UnaryOpKind.DateToTime")] // new
        [InlineData("D", "d", "UnaryOpKind.DateToDateTime")] // new
        [InlineData("n", "$", "UnaryOpKind.NumberToCurrency")] // new
        [InlineData("$", "n", "UnaryOpKind.CurrencyToNumber")] // new
        [InlineData("$", "s", "UnaryOpKind.CurrencyToText")] // new
        [InlineData("s", "$", "UnaryOpKind.TextToCurrency")] // new
        [InlineData("$", "b", "UnaryOpKind.CurrencyToBoolean")] // new
        [InlineData("b", "$", "UnaryOpKind.BooleanToCurrency")] // new
        [InlineData("w", "n", "function:Float")]
        [InlineData("w", "s", "UnaryOpKind.DecimalToText")]
        [InlineData("w", "b", "UnaryOpKind.DecimalToBoolean")]
        [InlineData("w", "d", "UnaryOpKind.DecimalToDateTime")]
        [InlineData("w", "D", "UnaryOpKind.DecimalToDate")]
        [InlineData("w", "T", "UnaryOpKind.DecimalToTime")]
        [InlineData("n", "w", "function:Decimal")]
        [InlineData("s", "w", "function:Decimal")]
        [InlineData("b", "w", "UnaryOpKind.BooleanToDecimal")]
        [InlineData("d", "w", "UnaryOpKind.DateTimeToDecimal")]
        [InlineData("D", "w", "UnaryOpKind.DateToDecimal")]
        [InlineData("T", "w", "UnaryOpKind.TimeToDecimal")]
        public void TestTypeCoercion(string fromTypeSpec, string toTypeSpec, string coercionKindStr)
        {
            Assert.True(DType.TryParse(fromTypeSpec, out var fromType));
            Assert.True(DType.TryParse(toTypeSpec, out var toType));

            UnaryOpKind unaryOpCoercionKind = (UnaryOpKind)(-1);
            string functionName = null;

            if (coercionKindStr.StartsWith("UnaryOpKind."))
            {
                unaryOpCoercionKind =
                    Enum.Parse<UnaryOpKind>(
                        coercionKindStr.Substring("UnaryOpKind.".Length));
            }
            else if (coercionKindStr.StartsWith("function:"))
            {
                functionName = coercionKindStr.Substring("function:".Length);
            }
            else
            {
                Assert.False(true, "Invalid coercionKindStr: " + coercionKindStr);
            }

            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("fromTypeVar", new TestFormulaType(fromType));
            symbolTable.AddVariable("toTypeVar", new TestFormulaType(toType));
            var config = new PowerFxConfig(Features.PowerFxV1)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var expression = "If(1<0, toTypeVar, fromTypeVar)";
            var result = engine.Check(expression);
            Assert.True(result.IsSuccess);

            var ir = result.ApplyIR();

            var callNode = ir.TopNode as CallNode;
            Assert.NotNull(callNode);
            Assert.Equal(3, callNode.Args.Count);

            var elseNode = callNode.Args[2] as LazyEvalNode;
            Assert.NotNull(elseNode);

            if (functionName != null)
            {
                var conversionCallNode = elseNode.Child as CallNode;
                Assert.NotNull(conversionCallNode);
                Assert.Equal(functionName, conversionCallNode.Function.Name);
            }
            else
            {
                if (elseNode.Child is ResolvedObjectNode)
                {
                    Assert.True(false, $"Conversion from {fromTypeSpec} to {toTypeSpec} should have a coercion node!");
                }

                var unaryNode = elseNode.Child as UnaryOpNode;
                Assert.NotNull(unaryNode);
                Assert.Equal(unaryOpCoercionKind, unaryNode.Op);
            }
        }

        private static void AssertEqual<T>(T a, PrimitiveValue<T> b)
        {
            Assert.Equal(a, b.Value);
        }

        private static void AssertEqual<T>(PrimitiveValue<T> a, PrimitiveValue<T> b)
        {
            Assert.Equal(a.Value, b.Value);
        }

        private static Engine GetEngineWithFeatureGatedFunctions()
        {
            SymbolTable symbol = new SymbolTable();
            symbol.AddFunctions(BuiltinFunctionsCore._featureGateFunctions);
            return new Engine(new PowerFxConfig() { SymbolTable = symbol });
        }
    }
}
