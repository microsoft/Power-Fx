// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class DTypeToJsonSchemaTests
    {
        // Helper method to access private DTypeToJsonSchema method via reflection
        private static string InvokeDTypeToJsonSchema(DType dType)
        {
            var copilotFunctionImplType = typeof(FormulaValueJSON).Assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == "CopilotFunctionImpl");

            Assert.NotNull(copilotFunctionImplType);

            var method = copilotFunctionImplType.GetMethod(
                "DTypeToJsonSchema",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { dType });
            return result as string;
        }

        // Helper to parse and validate JSON schema
        private static JsonNode ParseSchema(string jsonSchema)
        {
            var node = JsonNode.Parse(jsonSchema);
            Assert.NotNull(node);
            return node;
        }

        [Fact]
        public void DTypeToJsonSchema_Boolean()
        {
            var schema = InvokeDTypeToJsonSchema(DType.Boolean);
            var json = ParseSchema(schema);

            Assert.Equal("boolean", json["type"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_Number()
        {
            var schema = InvokeDTypeToJsonSchema(DType.Number);
            var json = ParseSchema(schema);

            Assert.Equal("number", json["type"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_Decimal()
        {
            var schema = InvokeDTypeToJsonSchema(DType.Decimal);
            var json = ParseSchema(schema);

            Assert.Equal("number", json["type"]?.ToString());
            Assert.Equal("decimal", json["format"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_String()
        {
            var schema = InvokeDTypeToJsonSchema(DType.String);
            var json = ParseSchema(schema);

            Assert.Equal("string", json["type"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_Date()
        {
            var schema = InvokeDTypeToJsonSchema(DType.Date);
            var json = ParseSchema(schema);

            Assert.Equal("string", json["type"]?.ToString());
            Assert.Equal("date", json["format"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_DateTime()
        {
            var schema = InvokeDTypeToJsonSchema(DType.DateTime);
            var json = ParseSchema(schema);

            Assert.Equal("string", json["type"]?.ToString());
            Assert.Equal("date-time", json["format"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_EmptyRecord()
        {
            var schema = InvokeDTypeToJsonSchema(DType.EmptyRecord);
            var json = ParseSchema(schema);

            Assert.Equal("object", json["type"]?.ToString());
            Assert.Null(json["properties"]);
            Assert.Null(json["required"]);
        }

        [Fact]
        public void DTypeToJsonSchema_SimpleRecord()
        {
            var recordType = DType.CreateRecord(
                new TypedName(DType.String, new DName("name")),
                new TypedName(DType.Number, new DName("age")));

            var schema = InvokeDTypeToJsonSchema(recordType);
            var json = ParseSchema(schema);

            Assert.Equal("object", json["type"]?.ToString());
            
            var properties = json["properties"] as JsonObject;
            Assert.NotNull(properties);
            Assert.Equal(2, properties.Count);
            
            Assert.Equal("string", properties["name"]?["type"]?.ToString());
            Assert.Equal("number", properties["age"]?["type"]?.ToString());

            var required = json["required"] as JsonArray;
            Assert.NotNull(required);
            Assert.Equal(2, required.Count);
            Assert.Contains(required, r => r?.ToString() == "name");
            Assert.Contains(required, r => r?.ToString() == "age");
        }

        [Fact]
        public void DTypeToJsonSchema_EmptyTable()
        {
            var schema = InvokeDTypeToJsonSchema(DType.EmptyTable);
            var json = ParseSchema(schema);

            Assert.Equal("array", json["type"]?.ToString());
            
            var items = json["items"] as JsonObject;
            Assert.NotNull(items);
            Assert.Equal("object", items["type"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_SimpleTable()
        {
            var tableType = DType.CreateTable(
                new TypedName(DType.String, new DName("name")),
                new TypedName(DType.Number, new DName("age")));

            var schema = InvokeDTypeToJsonSchema(tableType);
            var json = ParseSchema(schema);

            Assert.Equal("array", json["type"]?.ToString());
            
            var items = json["items"] as JsonObject;
            Assert.NotNull(items);
            Assert.Equal("object", items["type"]?.ToString());
            
            var properties = items["properties"] as JsonObject;
            Assert.NotNull(properties);
            Assert.Equal("string", properties["name"]?["type"]?.ToString());
            Assert.Equal("number", properties["age"]?["type"]?.ToString());
        }

        [Fact]
        public void DTypeToJsonSchema_StringEnum()
        {
            var enumType = DType.CreateEnum(
                DType.String,
                new KeyValuePair<DName, object>(new DName("Red"), "red"),
                new KeyValuePair<DName, object>(new DName("Green"), "green"),
                new KeyValuePair<DName, object>(new DName("Blue"), "blue"));

            var schema = InvokeDTypeToJsonSchema(enumType);
            var json = ParseSchema(schema);

            Assert.Equal("string", json["type"]?.ToString());
            
            var enumValues = json["enum"] as JsonArray;
            Assert.NotNull(enumValues);
            Assert.Equal(3, enumValues.Count);
            Assert.Contains(enumValues, e => e?.ToString() == "red");
            Assert.Contains(enumValues, e => e?.ToString() == "green");
            Assert.Contains(enumValues, e => e?.ToString() == "blue");
        }

        [Fact]
        public void DTypeToJsonSchema_InvalidDType_ThrowsException()
        {
            Assert.Throws<TargetInvocationException>(() =>
            {
                InvokeDTypeToJsonSchema(DType.Invalid);
            });
        }

        [Fact]
        public void DTypeToJsonSchema_OutputIsValidJson()
        {
            // Test that the output is always valid JSON
            var testTypes = new[]
            {
                DType.Boolean,
                DType.Number,
                DType.String,
                DType.EmptyRecord,
                DType.EmptyTable,
                DType.CreateRecord(new TypedName(DType.String, new DName("field"))),
                DType.CreateTable(new TypedName(DType.Number, new DName("value")))
            };

            foreach (var dType in testTypes)
            {
                var schema = InvokeDTypeToJsonSchema(dType);
                
                // Should not throw
                var parsed = JsonDocument.Parse(schema);
                Assert.NotNull(parsed);
            }
        }
    }
}
