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
            yield return (7, new TestUO(1.3f), @"""1.2999999523162842""");
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
            yield return (19, new TestUO(new { x = new { y = new[] { 1 }, z = "a", t = new { } }, a = false }), @"""{""""x"""":{""""y"""":[1],""""z"""":""""a"""",""""t"""":{}},""""a"""":false}""");
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
                int or decimal or double or float => new ExternalType(ExternalTypeKind.UntypedNumber),
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
                return _o is bool b ? b : throw new Exception("Not a boolean");
            }

            public decimal GetDecimal()
            {
                return _o is decimal d ? d : throw new Exception("Not a decimal");
            }

            public double GetDouble()
            {
                return _o is int i ? (double)i 
                     : _o is decimal d ? (double)d
                     : _o is float f ? (double)f
                     : _o is double dbl ? dbl : throw new Exception("Not a double");
            }

            public string GetString()
            {
                return _o == null ? null 
                     : _o is string str ? str : throw new Exception("Not a string");
            }

            public string GetUntypedNumber()
            {
                throw new System.NotImplementedException();
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
