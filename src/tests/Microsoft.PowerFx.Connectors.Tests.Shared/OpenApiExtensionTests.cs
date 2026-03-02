// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class OpenApiExtensionTests : PowerFxTest
    {
        [Theory]
        [InlineData("https://www.foo.bar", "www.foo.bar", "/")]
        [InlineData("https://www.FOO.bar", "www.foo.bar", "/")]
        [InlineData("http://www.foo.bar", null, null)]
        [InlineData("https://www.foo.bar:117", "www.foo.bar:117", "/")]
        [InlineData("https://www.foo.bar/xyz", "www.foo.bar", "/xyz")]
        [InlineData("https://www.FOO.BAR:2883/xyz/ABC", "www.foo.bar:2883", "/xyz/ABC")]
        [InlineData("https://localhost:44/efgh", "localhost:44", "/efgh")]
        [InlineData(null, null, null)]
        public void OpenApiExtension_Get(string url, string expectedAuthority, string expectedBasePath)
        {
            var doc = new OpenApiDocument();

            if (!string.IsNullOrEmpty(url))
            {
                var srv = new OpenApiServer { Url = url };
                doc.Servers.Add(srv);
            }

            Assert.Equal(expectedAuthority, doc.GetAuthority(null));
            Assert.Equal(expectedBasePath, doc.GetBasePath(null));
        }

        [Fact]
        public void OpenApiExtension_Null()
        {
            Assert.Null((null as OpenApiDocument).GetAuthority(null));
            Assert.Null((null as OpenApiDocument).GetBasePath(null));
        }

        [Fact]
        public void OpenApiExtension_MultipleServers()
        {
            var doc = new OpenApiDocument();
            var srv1 = new OpenApiServer { Url = "https://server1" };
            var srv2 = new OpenApiServer { Url = "https://server2" };
            doc.Servers.Add(srv1);
            doc.Servers.Add(srv2);

            // string str = doc.SerializeAsJson(OpenApi.OpenApiSpecVersion.OpenApi3_0);

            //{
            //    "openapi": "3.0.1",
            //      "info": { },
            //      "servers": [
            //        {
            //           "url": "https://server1"
            //        },
            //        {
            //           "url": "https://server2"
            //        }
            //      ],
            //      "paths": { }
            //}

            ConnectorErrors errors = new ConnectorErrors();

            doc.GetAuthority(errors);
            Assert.True(errors.HasErrors);
            Assert.Equal("Multiple servers in OpenApiDocument is not supported", errors.Errors.First());

            errors = new ConnectorErrors();
            doc.GetBasePath(errors);
            Assert.True(errors.HasErrors);
            Assert.Equal("Multiple servers in OpenApiDocument is not supported", errors.Errors.First());
        }

        [Fact]
        public void OpenApiExtension_ToRecord()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "b", FormulaType.String),
                 ("c", "d", FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`b:s, c`d:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord2()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "c", FormulaType.String),
                 ("c", "d", FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`c_1:s, c`d:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord3()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "d", FormulaType.String),
                 ("c", "d", FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`d:s, c`d_1:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord4()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "c", FormulaType.String),
                 ("c", "c", FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`c_1:s, c:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord5()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "b", FormulaType.String),
                 ("b", "a", FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`b_1:s, b`a_1:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord6()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "c", FormulaType.String),
                 ("c", null, FormulaType.Number)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`c_1:s, c:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord7()
        {
            var dict = new List<(string, string, FormulaType)>
            {
                 ("a", "c", FormulaType.String),
                 ("c", "c", FormulaType.Number),
                 ("b", "c", FormulaType.Decimal)
            };

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![a`c_1:s, b`c_2:w, c:n]", str);
        }

        [Fact]
        public void OpenApiExtension_ToRecord8()
        {
            var dict = new List<(string, string, FormulaType)>();

            RecordType record = dict.ToRecordType();
            string str = record.ToStringWithDisplayNames();

            Assert.Equal("![]", str);
        }

        [Theory]
        [InlineData("text/html", typeof(StringType), false)]
        [InlineData("application/pdf", typeof(UntypedObjectType), true)]
        public void GetConnectorReturnType_MediaTypeChecks(string mediaType, Type expectedType, bool expectError)
        {
            var operation = new OpenApiOperation
            {
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            [mediaType] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema { Type = "string" }
                            }
                        }
                    }
                }
            };

            var settings = new ConnectorSettings(null);
            var connectorType = OpenApiExtensions.GetConnectorReturnType(operation, settings);

            Assert.Equal(expectError, connectorType.HasErrors);
            Assert.IsType(expectedType, connectorType.FormulaType);
        }

        [Fact]
        public void GetConnectorType_NullTypeWithNoProperties_ReturnsUntypedObject()
        {
            // Test the case where schema.Type is null and schema.Properties is empty
            // This should return DefaultType (UntypedObject) as per the fix
            var schema = new OpenApiSchema
            {
                Type = null,
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            var swaggerParam = new SwaggerParameter("testParam", true, SwaggerSchema.New(schema), null);
            var settings = new ConnectorSettings(null);
            var connectorType = swaggerParam.GetConnectorType(settings);

            Assert.IsType<UntypedObjectType>(connectorType.FormulaType);
            Assert.False(connectorType.HasErrors);
        }

        [Fact]
        public void GetConnectorType_NullTypeWithProperties_ReturnsRecordType()
        {
            // Test that when Type is null but Properties exist, it still creates a RecordType
            var schema = new OpenApiSchema
            {
                Type = null,
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = "string" },
                    ["age"] = new OpenApiSchema { Type = "integer" }
                }
            };

            var swaggerParam = new SwaggerParameter("testParam", true, SwaggerSchema.New(schema), null);
            var settings = new ConnectorSettings(null);
            var connectorType = swaggerParam.GetConnectorType(settings);

            Assert.IsAssignableFrom<RecordType>(connectorType.FormulaType);
            Assert.False(connectorType.HasErrors);
        }

        [Fact]
        public void GetConnectorType_ObjectTypeWithoutProperties_ReturnsRecordType()
        {
            // Test that when Type is null but Properties exist, it still creates a RecordType
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            var swaggerParam = new SwaggerParameter("testParam", true, SwaggerSchema.New(schema), null);
            var settings = new ConnectorSettings(null);
            var connectorType = swaggerParam.GetConnectorType(settings);

            Assert.IsAssignableFrom<RecordType>(connectorType.FormulaType);
            Assert.False(connectorType.HasErrors);
        }

        [Theory]
        [InlineData(@"{""type"":""string""}", typeof(StringType))]
        [InlineData(@"{""type"":""number""}", typeof(DecimalType))]
        [InlineData(@"{""type"":""integer""}", typeof(DecimalType))]
        [InlineData(@"{""type"":""boolean""}", typeof(BooleanType))]
        public void JsonSchemaToFormulaType_Primitives(string jsonSchema, Type expectedType)
        {
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsType(expectedType, result);
        }

        [Theory]
        [InlineData(@"{""type"":""string"",""format"":""date""}", typeof(DateType))]
        [InlineData(@"{""type"":""string"",""format"":""date-time""}", typeof(DateTimeType))]
        [InlineData(@"{""type"":""string"",""format"":""binary""}", typeof(BlobType))]
        [InlineData(@"{""type"":""string"",""format"":""byte""}", typeof(BlobType))]
        [InlineData(@"{""type"":""string"",""format"":""uri""}", typeof(StringType))]
        public void JsonSchemaToFormulaType_StringFormats(string jsonSchema, Type expectedType)
        {
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_Object()
        {
            string jsonSchema = @"{""type"":""object"",""properties"":{""name"":{""type"":""string""},""age"":{""type"":""integer""}}}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<RecordType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_ObjectEmptyProperties()
        {
            string jsonSchema = @"{""type"":""object"",""properties"":{}}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<RecordType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_NestedObject()
        {
            string jsonSchema = @"{""type"":""object"",""properties"":{""address"":{""type"":""object"",""properties"":{""city"":{""type"":""string""}}}}}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<RecordType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_ArrayOfObjects()
        {
            string jsonSchema = @"{""type"":""array"",""items"":{""type"":""object"",""properties"":{""id"":{""type"":""integer""}}}}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<TableType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_ArrayOfPrimitives()
        {
            string jsonSchema = @"{""type"":""array"",""items"":{""type"":""string""}}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<TableType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_File()
        {
            string jsonSchema = @"{""type"":""file""}";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsType<BlobType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_UnsupportedType_ReturnsUntypedObject()
        {
            // A non-object JSON root element (e.g. just a string literal) returns null from SwaggerJsonSchema.New
            string jsonSchema = @"""just a string""";
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsType<UntypedObjectType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_NullInput_ThrowsArgumentException()
        {
            Assert.ThrowsAny<ArgumentException>(() => OpenApiExtensions.JsonSchemaToFormulaType(null));
        }

        [Fact]
        public void JsonSchemaToFormulaType_EmptyInput_ThrowsArgumentException()
        {
            Assert.ThrowsAny<ArgumentException>(() => OpenApiExtensions.JsonSchemaToFormulaType(string.Empty));
        }

        [Fact]
        public void JsonSchemaToFormulaType_InvalidJson_ThrowsJsonException()
        {
            Assert.ThrowsAny<JsonException>(() => OpenApiExtensions.JsonSchemaToFormulaType("{not valid json}"));
        }

        [Fact]
        public void JsonSchemaToFormulaType_WithSettings()
        {
            string jsonSchema = @"{""type"":""string""}";
            var settings = new ConnectorSettings(null);
            FormulaType result = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema, settings);
            Assert.IsType<StringType>(result);
        }

        [Fact]
        public void JsonSchemaToFormulaType_NullSettings_ThrowsArgumentException()
        {
            Assert.ThrowsAny<ArgumentException>(() => OpenApiExtensions.JsonSchemaToFormulaType(@"{""type"":""string""}", null));
        }

        [Fact]
        public void JsonSchemaToFormulaType_EndToEnd_WithLookUp()
        {
            // 1. Define the JSON schema for the case documents structure
            string jsonSchema = @"{
                ""required"": [""CaseNumber"", ""Documents""],
                ""properties"": {
                    ""CaseNumber"": { ""type"": ""string"" },
                    ""Documents"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Data"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""DocumentName"": { ""type"": ""string"" },
                                        ""FooterId"": { ""type"": ""string"" },
                                        ""Fields"": {
                                            ""type"": ""array"",
                                            ""items"": {
                                                ""type"": ""object"",
                                                ""properties"": {
                                                    ""FieldName"": { ""type"": ""string"" },
                                                    ""FieldValue"": { ""type"": ""string"" },
                                                    ""ConfidenceValue"": { ""type"": ""string"" }
                                                }
                                            }
                                        },
                                        ""Tables"": {
                                            ""type"": ""array"",
                                            ""items"": {
                                                ""type"": ""object"",
                                                ""properties"": {
                                                    ""Section"": { ""type"": ""string"" },
                                                    ""Rows"": {
                                                        ""type"": ""array"",
                                                        ""items"": {
                                                            ""type"": ""object"",
                                                            ""properties"": {
                                                                ""Fields"": {
                                                                    ""type"": ""array"",
                                                                    ""items"": {
                                                                        ""type"": ""object"",
                                                                        ""properties"": {
                                                                            ""FieldName"": { ""type"": ""string"" },
                                                                            ""FieldValue"": { ""type"": ""string"" },
                                                                            ""ConfidenceValue"": { ""type"": ""string"" }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }";

            // 2. Convert JSON schema to FormulaType
            FormulaType caseDocumentsType = OpenApiExtensions.JsonSchemaToFormulaType(jsonSchema);
            Assert.IsAssignableFrom<RecordType>(caseDocumentsType);

            // 3. Create a parameter type wrapping it as "CaseDocuments"
            RecordType parameterType = RecordType.Empty().Add("CaseDocuments", caseDocumentsType);

            // 4. Parse JSON data using FormulaValueJSON.FromJson with the generated type
            string jsonData = @"{
                ""CaseNumber"": ""5-0000014066889"",
                ""Documents"": [
                    {
                        ""Data"": {
                            ""DocumentName"": ""Previous Agreement Form"",
                            ""FooterId"": ""prevenragrform"",
                            ""Fields"": [],
                            ""Tables"": []
                        }
                    },
                    {
                        ""Data"": {
                            ""DocumentName"": ""Signature Form"",
                            ""FooterId"": ""ProgramSignForm"",
                            ""Fields"": [
                                { ""FieldName"": ""Proposal ID"", ""FieldValue"": ""7-3EJSBH65M5"", ""ConfidenceValue"": """" },
                                { ""FieldName"": ""Agreement Number"", ""FieldValue"": ""47276518"", ""ConfidenceValue"": """" }
                            ],
                            ""Tables"": [
                                {
                                    ""Section"": ""Contract Documents"",
                                    ""Rows"": [
                                        {
                                            ""Fields"": [
                                                { ""FieldName"": ""PO Number"", ""FieldValue"": ""PO-12345"", ""ConfidenceValue"": ""0.95"" }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    }
                ]
            }";

            FormulaValue caseDocumentsValue = FormulaValueJSON.FromJson(jsonData, caseDocumentsType);
            Assert.IsAssignableFrom<RecordValue>(caseDocumentsValue);

            // 5. Build the parameter record wrapping CaseDocuments
            RecordValue parameters = FormulaValue.NewRecordFromFields(
                new NamedValue("CaseDocuments", caseDocumentsValue));

            // 6. Evaluate the Power Fx expression
            var engine = new RecalcEngine();
            string expression = @"With(
                {
                    caseNumber: CaseDocuments.CaseNumber,
                    contract: LookUp(
                        LookUp(CaseDocuments.Documents, Data.DocumentName = ""Signature Form"").Data.Tables,
                        Section = ""Contract Documents""
                    )
                },
                {
                    results: contract
                }
            )";

            FormulaValue result = engine.Eval(expression, parameters);

            Assert.IsAssignableFrom<RecordValue>(result);
            RecordValue resultRecord = (RecordValue)result;

            // The "results" field should contain the matched table row
            FormulaValue resultsField = resultRecord.GetField("results");
            Assert.IsAssignableFrom<RecordValue>(resultsField);

            RecordValue contractRecord = (RecordValue)resultsField;
            FormulaValue sectionValue = contractRecord.GetField("Section");
            Assert.Equal("Contract Documents", ((StringValue)sectionValue).Value);
        }
    }
}
