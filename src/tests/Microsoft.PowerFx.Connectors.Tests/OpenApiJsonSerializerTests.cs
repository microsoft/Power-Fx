// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class OpenApiJsonSerializerTests : PowerFxTest
    {
        private string SerializeJson(Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> parameters) => Serialize<OpenApiJsonSerializer>(parameters);

        private string Serialize<T>(Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> parameters)
            where T : FormulaValueSerializer
        {
            var jsonSerializer = (FormulaValueSerializer)Activator.CreateInstance(typeof(T), new object[] { false });
            jsonSerializer.StartSerialization(null);
            
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    jsonSerializer.SerializeValue(parameter.Key, parameter.Value.Schema, parameter.Value.Value);
                }
            }

            jsonSerializer.EndSerialization();
            return jsonSerializer.GetResult();
        }

        private TableValue GetArray(params double[] values)
        {
            return FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));
        }

        private TableValue GetArray(params string[] values)
        {
            return FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));
        }

        private TableValue GetTable(RecordValue recordValue)
        {
            return FormulaValue.NewTable(recordValue.Type, recordValue);
        }

        [Fact]
        public void JsonSerializer_Empty()
        {
            var str = SerializeJson(null);
            Assert.Equal("{}", str);
        }

        [Fact]
        public void JsonSerializer_SingleInteger()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "integer" }, FormulaValue.New(1))
            });

            Assert.Equal(@"{""a"":1}", str);
        }

        [Fact]
        public void JsonSerializer_EscapedKey()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a\\b\"c\""] = (new OpenApiSchema() { Type = "integer" }, FormulaValue.New(1))
            });

            Assert.Equal(@"{""a\\b\u0022c\u0022"":1}", str);
        }

        [Fact]
        public void JsonSerializer_SingleInteger_NoValue()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "integer" }, null)
            }));

            Assert.Equal("Expected NumberValue (integer) and got <null> value, for property a", ex.Message);           
        }

        [Fact]
        public void JsonSerializer_SingleInteger_NoSchema()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (null, null)
            }));
            
            Assert.Equal("Missing schema for property a", ex.Message);
        }

        [Fact]
        public void JsonSerializer_Integer_String_Mismatch()
        {
            var ex = Assert.Throws<ArgumentException>(() => SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "integer" }, FormulaValue.New("abc"))
            }));

            Assert.Equal("Expected NumberValue (integer) and got StringValue value, for property a", ex.Message);
        }

        [Fact]
        public void JsonSerializer_TwoIntegers()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "integer" }, FormulaValue.New(1)),
                ["b"] = (new OpenApiSchema() { Type = "integer" }, FormulaValue.New(-2))
            });
            
            Assert.Equal(@"{""a"":1,""b"":-2}", str);
        }

        [Fact]
        public void JsonSerializer_String()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "string" }, FormulaValue.New("abc"))
            });

            Assert.Equal(@"{""a"":""abc""}", str);
        }

        [Fact]
        public void JsonSerializer_Bool()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "boolean" }, FormulaValue.New(true)),
                ["b"] = (new OpenApiSchema() { Type = "boolean" }, FormulaValue.New(false))
            });
            
            Assert.Equal(@"{""a"":true,""b"":false}", str);
        }

        [Fact]
        public void JsonSerializer_Array()
        {            
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "array", Format = "int" }, GetArray(1, 2))
            });

            Assert.Equal(@"{""a"":[1,2]}", str);
        }

        [Fact]
        public void JsonSerializer_Array_String()
        {
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "array", Format = "string" }, GetArray("abc", "def"))
            });

            Assert.Equal(@"{""a"":[""abc"",""def""]}", str);
        }

        [Fact]
        public void JsonSerializer_Array_Invalid()
        {            
            var ex = Assert.Throws<ArgumentException>(() => SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["a"] = (new OpenApiSchema() { Type = "array", Format = "int" }, GetTable(FormulaValue.NewRecordFromFields(new NamedValue("a", FormulaValue.New(1)), new NamedValue("b", FormulaValue.New("foo")))))
            }));

            Assert.Equal("Incompatible Table for supporting array, RecordValue has more than one column - propertyName a, number of fields 2", ex.Message);
        }

        [Theory]
        [InlineData("2022-06-21T14:36:59.9353993+02:00")]
        [InlineData("2022-06-21T14:36:59.9353993-08:00")]
        public void JsonSerializer_Date(string dateString)
        {
            var date = DateTime.Parse(dateString);
            var str = SerializeJson(new Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)>()
            {
                ["A"] = (new OpenApiSchema() { Type = "string", Format = "date-time" }, FormulaValue.New(date))
            });

            var obj = JsonSerializer.Deserialize<DateTimeType>(str);
            Assert.Equal(date, obj.A);
        }

        private class DateTimeType
        {
            public DateTime A { get; set; }
        }
    }
}
