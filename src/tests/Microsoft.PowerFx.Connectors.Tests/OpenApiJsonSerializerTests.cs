// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class OpenApiJsonSerializerTests : PowerFxTest
    {
        [Fact]
        public void JsonSerializer_Empty()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema();
            var fields = new List<NamedValue>();

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal("{}", str);
        }

        [Fact]
        public void JsonSerializer_SingleInteger()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "integer" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.New(1)) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal(@"{""a"":1}", str);
        }

        [Fact]
        public void JsonSerializer_SingleInteger_NoField()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "integer" } } };
            var fields = new List<NamedValue>();

            var ex = Assert.Throws<NotImplementedException>(() => jsonSerializer.Serialize(schema, fields));
            Assert.Equal("Missing property a, object is too complex or not supported", ex.Message);
        }

        [Fact]
        public void JsonSerializer_SingleInteger_InvalidValue()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "integer" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", null) };

            var ex = Assert.Throws<ArgumentException>(() => jsonSerializer.Serialize(schema, fields));
            Assert.Equal("Missing valid FormulaValue for property a", ex.Message);
        }

        [Fact]
        public void JsonSerializer_Integer_String_Mismatch()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "integer" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.New("abc")) };

            var ex = Assert.Throws<ArgumentException>(() => jsonSerializer.Serialize(schema, fields));
            Assert.Equal("Type mismatch for property a, expected type Number and got String", ex.Message);
        }

        [Fact]
        public void JsonSerializer_TwoIntegers()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "integer" }, ["b"] = new OpenApiSchema() { Type = "integer" } } };
            var fields = new List<NamedValue>() { new NamedValue("b", FormulaValue.New(-2)), new NamedValue("a", FormulaValue.New(1)) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal(@"{""a"":1,""b"":-2}", str);
        }

        [Fact]
        public void JsonSerializer_String()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "string" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.New("abc")) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal(@"{""a"":""abc""}", str);
        }

        [Fact]
        public void JsonSerializer_Bool()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "boolean" }, ["b"] = new OpenApiSchema() { Type = "boolean" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.New(true)), new NamedValue("b", FormulaValue.New(false)) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal(@"{""a"":true,""b"":false}", str);
        }

        [Fact]
        public void JsonSerializer_Array()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "array", Format = "int" } } };                            
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.NewSingleColumnTable(FormulaValue.New(1), FormulaValue.New(2))) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal(@"{""a"":[1,2]}", str);
        }

        [Fact]
        public void JsonSerializer_Array_Invalid()
        {
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "array", Format = "int" } } };
            var record = FormulaValue.NewRecordFromFields(new NamedValue("a", FormulaValue.New(1)), new NamedValue("b", FormulaValue.New("foo")));
            var table = new InMemoryTableValue(IRContext.NotInSource(TableType.FromRecord(record.Type)), new List<RecordValue>() { record }.Select(r => DValue<RecordValue>.Of(r)));
            var fields = new List<NamedValue>() { new NamedValue("a", table) };

            var ex = Assert.Throws<ArgumentException>(() => jsonSerializer.Serialize(schema, fields));
            Assert.Equal("Incompatible Table for supporting array, RecordValue has more than one column - propertyName a, number of fields 2", ex.Message);
        }

        [Fact]
        public void JsonSerializer_Date()
        {
            var now = DateTime.Now;
            var jsonSerializer = new OpenApiJsonSerializer();
            var schema = new OpenApiSchema() { Properties = new Dictionary<string, OpenApiSchema>() { ["a"] = new OpenApiSchema() { Type = "string", Format = "date-time" } } };
            var fields = new List<NamedValue>() { new NamedValue("a", FormulaValue.New(now)) };

            var str = jsonSerializer.Serialize(schema, fields);
            Assert.Equal($@"{{""a"":""{now.ToString("o", CultureInfo.InvariantCulture).Replace("+", "\u002B")}""}}", str);
        }
    }
}
