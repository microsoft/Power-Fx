// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

#pragma warning disable CA1065

namespace Microsoft.PowerFx.Tests
{
    public class JsonSerializeUOTests : PowerFxTest
    {
        [Fact]
        public async Task JsonSerializeUOTest()
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableJsonFunctions();

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot objSlot = symbolTable.AddVariable("obj", FormulaType.UntypedObject);

            foreach ((int id, TestUO uo, string expectedResult) in GetUOTests())
            {
                SymbolValues symbolValues = new SymbolValues(symbolTable);
                symbolValues.Set(objSlot, FormulaValue.New(uo));

                RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues);
                RecalcEngine engine = new RecalcEngine(config);

                FormulaValue fv = await engine.EvalAsync("JSON(obj)", CancellationToken.None, runtimeConfig: runtimeConfig);
                Assert.IsNotType<ErrorValue>(fv);

                string str = fv.ToExpression().ToString();
                Assert.True(expectedResult == str, $"[{id}: Expected={expectedResult}, Result={str}]");
            }
        }

        private IEnumerable<(int id, TestUO uo, string expectedResult)> GetUOTests()
        {
            yield return (1, new TestUO(true), @"""true""");
            yield return (2, new TestUO(false), @"""false""");
            yield return (3, new TestUO(string.Empty), @"""""""""""""");
            yield return (4, new TestUO("abc"), @"""""""abc""""""");
            yield return (5, new TestUO(null), @"""null""");
            yield return (6, new TestUO(0), @"""0""");
            yield return (7, new TestUO(1.3f), @"""1.3""");            
            yield return (8, new TestUO(-1.7m), @"""-1.7""");
            yield return (9, new TestUO(new[] { true, false }), @"""[true,false]""");
            yield return (10, new TestUO(new bool[0]), @"""[]""");
            yield return (11, new TestUO(new[] { "abc", "def" }), @"""[""""abc"""",""""def""""]""");
            yield return (12, new TestUO(new string[0]), @"""[]""");
            yield return (13, new TestUO(new[] { 11.5m, -7.5m }), @"""[11.5,-7.5]""");
            yield return (14, new TestUO(new string[0]), @"""[]""");
            yield return (15, new TestUO(new[] { new[] { 1, 2 }, new[] { 3, 4 } }), @"""[[1,2],[3,4]]""");
            yield return (16, new TestUO(new[] { new object[] { 1, 2 }, new object[] { true, "a", 7 } }), @"""[[1,2],[true,""""a"""",7]]""");
            yield return (17, new TestUO(new { a = 10, b = -20m, c = "abc" }), @"""{""""a"""":10,""""b"""":-20,""""c"""":""""abc""""}""");
            yield return (18, new TestUO(new { x = new { y = true } }), @"""{""""x"""":{""""y"""":true}}""");
            yield return (19, new TestUO(new { x = new { y = new[] { 1 }, z = "a", t = new { } }, a = false }), @"""{""""a"""":false,""""x"""":{""""t"""":{},""""y"""":[1],""""z"""":""""a""""}}""");
            yield return (20, new TestUO(123456789012345.6789012345678m), @"""123456789012345.6789012345678""");
        }

        [Fact]
        public async Task JsonSerializeUOWithFlattenValueTablesTest()
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableJsonFunctions();

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot objSlot = symbolTable.AddVariable("obj", FormulaType.UntypedObject);
            RecalcEngine engine = new RecalcEngine(config);

            // Test 1: Single-column "Value" table should be flattened
            var singleColumnValueArray = new[]
            {
                new { Value = 1 },
                new { Value = 2 },
                new { Value = 3 }
            };

            SymbolValues symbolValues1 = new SymbolValues(symbolTable);
            symbolValues1.Set(objSlot, FormulaValue.New(new TestUO(singleColumnValueArray)));
            RuntimeConfig runtimeConfig1 = new RuntimeConfig(symbolValues1);

            // With FlattenValueTables - should flatten to [1,2,3]
            FormulaValue fv1 = await engine.EvalAsync("JSON(obj, JSONFormat.FlattenValueTables)", CancellationToken.None, runtimeConfig: runtimeConfig1);
            Assert.IsNotType<ErrorValue>(fv1);
            string str1 = fv1.ToExpression().ToString();
            Assert.Equal(@"""[1,2,3]""", str1);

            // Without FlattenValueTables - should keep structure
            FormulaValue fv2 = await engine.EvalAsync("JSON(obj)", CancellationToken.None, runtimeConfig: runtimeConfig1);
            Assert.IsNotType<ErrorValue>(fv2);
            string str2 = fv2.ToExpression().ToString();
            Assert.Equal(@"""[{""""Value"""":1},{""""Value"""":2},{""""Value"""":3}]""", str2);

            // Test 2: Multi-column table should NOT be flattened even with FlattenValueTables
            var multiColumnArray = new[]
            {
                new { a = 1, b = 2 },
                new { a = 3, b = 4 }
            };

            SymbolValues symbolValues2 = new SymbolValues(symbolTable);
            symbolValues2.Set(objSlot, FormulaValue.New(new TestUO(multiColumnArray)));
            RuntimeConfig runtimeConfig2 = new RuntimeConfig(symbolValues2);

            FormulaValue fv3 = await engine.EvalAsync("JSON(obj, JSONFormat.FlattenValueTables)", CancellationToken.None, runtimeConfig: runtimeConfig2);
            Assert.IsNotType<ErrorValue>(fv3);
            string str3 = fv3.ToExpression().ToString();
            Assert.Equal(@"""[{""""a"""":1,""""b"""":2},{""""a"""":3,""""b"""":4}]""", str3);

            // Test 3: Test with null/blank values in Value column
            var arrayWithNulls = new[]
            {
                new { Value = (int?)1 },
                new { Value = (int?)null },
                new { Value = (int?)3 }
            };

            SymbolValues symbolValues3 = new SymbolValues(symbolTable);
            symbolValues3.Set(objSlot, FormulaValue.New(new TestUO(arrayWithNulls)));
            RuntimeConfig runtimeConfig3 = new RuntimeConfig(symbolValues3);

            FormulaValue fv4 = await engine.EvalAsync("JSON(obj, JSONFormat.FlattenValueTables)", CancellationToken.None, runtimeConfig: runtimeConfig3);
            Assert.IsNotType<ErrorValue>(fv4);
            string str4 = fv4.ToExpression().ToString();
            Assert.Equal(@"""[1,null,3]""", str4);

            // Test 4: Mixed array with multi-column object first, then single-column Value objects
            var mixedArray = new object[]
            {
                new { Value = 2, Value2 = 22 },
                new { Value = 1 },
                new { Value = 3 }
            };

            SymbolValues symbolValues4 = new SymbolValues(symbolTable);
            symbolValues4.Set(objSlot, FormulaValue.New(new TestUO(mixedArray)));
            RuntimeConfig runtimeConfig4 = new RuntimeConfig(symbolValues4);

            FormulaValue fv5 = await engine.EvalAsync("JSON(obj, JSONFormat.FlattenValueTables)", CancellationToken.None, runtimeConfig: runtimeConfig4);
            Assert.IsNotType<ErrorValue>(fv5);
            string str5 = fv5.ToExpression().ToString();
            // Should flatten only the single-property Value objects, keep the multi-property object as-is
            Assert.Equal(@"""[{""""Value"""":2,""""Value2"""":22},1,3]""", str5);
        }

        public class TestUO : IUntypedObject
        {
            private enum UOType
            {
                Unknown = -1,
                Array,
                Object,
                Bool,
                Decimal,
                String
            }
            
            private readonly dynamic _o;

            public TestUO(object o)
            {                
                _o = o;
            }
            
            public IUntypedObject this[int index] => GetUOType(_o) == UOType.Array ? new TestUO(_o[index]) : throw new Exception("Not an array");

            public FormulaType Type => _o == null ? FormulaType.String : _o switch
            {
                string => FormulaType.String,
                int or double or float => new ExternalType(ExternalTypeKind.UntypedNumber),
                decimal => FormulaType.Decimal,
                bool => FormulaType.Boolean,
                Array => new ExternalType(ExternalTypeKind.Array),
                object o => new ExternalType(ExternalTypeKind.Object),                
                _ => throw new Exception("Not a valid type")
            };

            private static UOType GetUOType(object o) => o switch
            {
                bool => UOType.Bool,
                int or decimal or double => UOType.Decimal,
                string => UOType.String,
                Array => UOType.Array,
                _ => UOType.Object
            };

            public int GetArrayLength()
            {
                return _o is Array a ? a.Length : throw new Exception("Not an array");
            }

            public bool GetBoolean()
            {
                return _o is bool b ? b 
                     : throw new Exception("Not a boolean");
            }

            public decimal GetDecimal()
            {
                return _o is int i ? (decimal)i
                     : _o is float f ? (decimal)f
                     : _o is decimal dec ? dec 
                     : throw new Exception("Not a decimal");
            }

            public double GetDouble()
            {
                return _o is int i ? (double)i                      
                     : _o is float f ? (double)f
                     : _o is double dbl ? dbl 
                     : throw new Exception("Not a double");
            }

            public string GetString()
            {
                return _o == null ? null 
                     : _o is string str ? str 
                     : throw new Exception("Not a string");
            }

            public string GetUntypedNumber()
            {
                return _o is int i ? i.ToString()
                     : _o is float f ? f.ToString()
                     : _o is double dbl ? dbl.ToString()
                     : _o is decimal dec ? dec.ToString() 
                     : throw new Exception("Not valid untyped number");
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                if (_o is object o && o.GetType().GetProperties().Any(pi => pi.Name == value))
                {
                    PropertyInfo pi = o.GetType().GetProperty(value);
                    object prop = pi.GetValue(_o);                    

                    result = new TestUO(prop);
                    return true;
                }

                result = null;
                return false;
            }

            public bool TryGetPropertyNames(out IEnumerable<string> propertyNames)
            {
                if (_o is object o)
                {
                    propertyNames = o.GetType().GetProperties().Select(pi => pi.Name);
                    return true;
                }

                propertyNames = null;
                return false;
            }
        }
    }
}
