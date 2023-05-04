// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class OpenApiParserTests
    {
        [Fact]
        public void ACSL_GetFunctionNames()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language.json");

            // OpenAPI spec: Info.Title is required
            Assert.Equal("Azure Cognitive Service for Language", doc.Info.Title);

            // OpenAPI spec: Info.Description is optional
            Assert.Equal("Azure Cognitive Service for Language, previously known as 'Text Analytics' connector detects language, sentiment and more of the text you provide.", doc.Info.Description);

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(doc).OrderBy(cf => cf.Name).ToList();
            Assert.Equal(51, functions.Count);
            ConnectorFunction function = functions[19];

            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("Analyzes the input conversation utterance.", function.Description);
            Assert.Equal("Conversations (CLU) (2022-05-01)", function.Summary);
            Assert.Equal("/apim/cognitiveservicestextanalytics/{connectionId}/language/:analyze-conversations", function.OperationPath);
            Assert.Equal(HttpMethod.Post, function.HttpMethod);
        }

        [Fact]

        public void ACSL_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language.json");
            List<ServiceFunction> functionList = OpenApiParser.Parse("ACSL", doc);
            Assert.Contains(functionList, sf => sf.GetUniqueTexlRuntimeName() == "aCSL__ConversationAnalysisAnalyzeConversationConversation");
        }

#pragma warning disable SA1118, SA1137

        [Fact]
        public void ACSL_GetFunctionParameters()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language.json");
            ConnectorFunction function = OpenApiParser.GetFunctions(doc).OrderBy(cf => cf.Name).ToList()[19];

            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("ConversationAnalysis_AnalyzeConversation_Conversation", function.OriginalName);
            Assert.Equal("/apim/cognitiveservicestextanalytics/{connectionId}/language/:analyze-conversations", function.OperationPath);

            Assert.Equal(2, function.RequiredParameters.Length);
            Assert.Equal(3, function.HiddenRequiredParameters.Length);
            Assert.Empty(function.OptionalParameters);

            RecordType analysisInputRecordType = Extensions.MakeRecordType(
                                                    ("conversationItem", Extensions.MakeRecordType(
                                                        ("language", FormulaType.String),
                                                        ("text", FormulaType.String))));
            RecordType parametersRecordType = Extensions.MakeRecordType(
                                                    ("deploymentName", FormulaType.String),
                                                    ("projectName", FormulaType.String),
                                                    ("verbose", RecordType.Boolean));

            // -- Parameter 1 --
            Assert.Equal("analysisInput", function.RequiredParameters[0].Name);
            Assert.Equal(analysisInputRecordType, function.RequiredParameters[0].FormulaType);
            Assert.Equal("Body", function.RequiredParameters[0].Description);
            Assert.Null(function.RequiredParameters[0].DefaultValue);
            Assert.NotNull(function.RequiredParameters[0].ConnectorType);
            Assert.Equal("analysisInput", function.RequiredParameters[0].ConnectorType.Name);
            Assert.Null(function.RequiredParameters[0].ConnectorType.DisplayName);
            Assert.Equal("The input ConversationItem and its optional parameters", function.RequiredParameters[0].ConnectorType.Description);
            Assert.Equal(analysisInputRecordType, function.RequiredParameters[0].ConnectorType.FormulaType);
            Assert.True(function.RequiredParameters[0].ConnectorType.IsRequired);
            Assert.Single(function.RequiredParameters[0].ConnectorType.Fields);
            Assert.Equal("conversationItem", function.RequiredParameters[0].ConnectorType.Fields[0].Name);
            Assert.Null(function.RequiredParameters[0].ConnectorType.Fields[0].DisplayName);
            Assert.Equal("The abstract base for a user input formatted conversation (e.g., Text, Transcript).", function.RequiredParameters[0].ConnectorType.Fields[0].Description);
            Assert.True(function.RequiredParameters[0].ConnectorType.Fields[0].IsRequired);            

            // -- Parameter 2 --
            Assert.Equal("parameters", function.RequiredParameters[1].Name);
            Assert.Equal(parametersRecordType, function.RequiredParameters[1].FormulaType);
            Assert.Equal("Body", function.RequiredParameters[1].Description);
            Assert.Null(function.RequiredParameters[1].DefaultValue);

            RecordType analysisInputRecordTypeH = Extensions.MakeRecordType(
                                                    ("conversationItem", Extensions.MakeRecordType(
                                                        ("id", FormulaType.String),
                                                        ("participantId", FormulaType.String))));

            // -- Hidden Required Parameter 1 --
            Assert.Equal("api-version", function.HiddenRequiredParameters[0].Name);
            Assert.Equal(FormulaType.String, function.HiddenRequiredParameters[0].FormulaType);
            Assert.Equal("Client API version.", function.HiddenRequiredParameters[0].Description);
            Assert.Equal("2022-05-01", function.HiddenRequiredParameters[0].DefaultValue.ToObject());

            // -- Hidden Required Parameter 2 --
            Assert.Equal("kind", function.HiddenRequiredParameters[1].Name);
            Assert.Equal(FormulaType.String, function.HiddenRequiredParameters[1].FormulaType);
            Assert.Equal("Body", function.HiddenRequiredParameters[1].Description);
            Assert.Equal("Conversation", function.HiddenRequiredParameters[1].DefaultValue.ToObject());

            // -- Hidden Required Parameter 3 --
            Assert.Equal("analysisInput", function.HiddenRequiredParameters[2].Name);
            Assert.Equal(analysisInputRecordTypeH, function.HiddenRequiredParameters[2].FormulaType);
            Assert.Equal("Body", function.HiddenRequiredParameters[2].Description);
            Assert.Equal(@"{""conversationItem"":{""id"":""0"",""participantId"":""0""}}", System.Text.Json.JsonSerializer.Serialize(function.HiddenRequiredParameters[2].DefaultValue.ToObject()));

            Assert.Equal(2, function.ArityMin);
            Assert.Equal(2, function.ArityMax);

            // -- Return Type --
            FormulaType returnType = function.ReturnType;

            RecordType expectedReturnType = Extensions.MakeRecordType(
                                                ("kind", FormulaType.String),
                                                ("result", Extensions.MakeRecordType(
                                                    ("detectedLanguage", FormulaType.String),
                                                    ("prediction", Extensions.MakeRecordType(
                                                        ("entities", Extensions.MakeTableType(
                                                            ("category", FormulaType.String),
                                                            ("confidenceScore", FormulaType.Decimal),
                                                            ("extraInformation", FormulaType.UntypedObject), // property has a discriminator
                                                            ("length", FormulaType.Decimal),
                                                            ("offset", FormulaType.Decimal),
                                                            ("resolutions", FormulaType.UntypedObject),      // property has a discriminator
                                                            ("text", FormulaType.String))),
                                                        ("intents", Extensions.MakeTableType(
                                                            ("category", FormulaType.String),
                                                            ("confidenceScore", FormulaType.Decimal))),
                                                        ("projectKind", FormulaType.String),
                                                        ("topIntent", FormulaType.String))),
                                                        ("query", FormulaType.String))));

            string rt3Name = expectedReturnType.ToStringWithDisplayNames();
            string returnTypeName = expectedReturnType.ToStringWithDisplayNames();

            Assert.Equal(rt3Name, returnTypeName);
            Assert.True((FormulaType)expectedReturnType == returnType);

            ConnectorType connectorReturnType = function.ConnectorReturnType;
            Assert.NotNull(connectorReturnType);
            Assert.Equal((FormulaType)expectedReturnType, connectorReturnType.FormulaType);
            Assert.Equal(2, connectorReturnType.Fields.Length);
            Assert.Equal("The results of a Conversation task.", connectorReturnType.Description);            
        }
#pragma warning restore SA1118, SA1137

        [Fact]
        public async Task ACSL_InvokeFunction()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.PowerFxV1);
            ConnectorFunction function = OpenApiParser.GetFunctions(apiDoc).OrderBy(cf => cf.Name).ToList()[19];
            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("![kind:s, result:![detectedLanguage:s, prediction:![entities:*[category:s, confidenceScore:w, extraInformation:O, length:w, offset:w, resolutions:O, text:s], intents:*[category:s, confidenceScore:w], projectKind:s, topIntent:s], query:s]]", function.ReturnType.ToStringWithDisplayNames());

            RecalcEngine engine = new RecalcEngine(pfxConfig);

            string analysisInput = @"{ conversationItem: { modality: ""text"", language: ""en-us"", text: ""Book me a flight for Munich"" } }";
            string parameters = @"{ deploymentName: ""deploy1"", projectName: ""project1"", verbose: true, stringIndexType: ""TextElement_V8"" }";
            FormulaValue analysisInputParam = engine.Eval(analysisInput);
            FormulaValue parametersParam = engine.Eval(parameters);

            using var httpClient = new HttpClient(testConnector);
            testConnector.SetResponseFromFile(@"Responses\Azure Cognitive Service for Language_Response.json");
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://lucgen-apim.azure-api.net", "aaa373836ffd4915bf6eefd63d164adc" /* environment Id */, "16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b" /* connectionId */, () => "No Auth", httpClient)
            {
                SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878",
            };

            FormulaValue httpResult = await function.InvokeAync(client, new FormulaValue[] { analysisInputParam, parametersParam }, CancellationToken.None).ConfigureAwait(false);
            httpClient.Dispose();
            client.Dispose();
            testConnector.Dispose();

            using var testConnector2 = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language.json");
            using var httpClient2 = new HttpClient(testConnector2);
            testConnector2.SetResponseFromFile(@"Responses\Azure Cognitive Service for Language_Response.json");
            using PowerPlatformConnectorClient client2 = new PowerPlatformConnectorClient("https://lucgen-apim.azure-api.net", "aaa373836ffd4915bf6eefd63d164adc" /* environment Id */, "16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b" /* connectionId */, () => "No Auth", httpClient2)
            {
                SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878",
            };

            FormulaValue httpResult2 = await function.InvokeAync(client2, new FormulaValue[] { analysisInputParam, parametersParam }, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(httpResult2);
            Assert.True(httpResult2 is RecordValue);

            RecordValue httpResultValue = (RecordValue)httpResult2;
            RecordValue resultValue = (RecordValue)httpResultValue.GetField("result");
            RecordValue predictionValue = (RecordValue)resultValue.GetField("prediction");
            TableValue entitiesValue = (TableValue)predictionValue.GetField("entities");
            RecordValue entityValue = (RecordValue)entitiesValue.Rows.First().Value;
            FormulaValue resolutionsValue = entityValue.GetField("resolutions");

            Assert.True(resolutionsValue is UntypedObjectValue);

            UntypedObjectValue resolutionUO = (UntypedObjectValue)resolutionsValue;
            IUntypedObject impl = resolutionUO.Impl;
            Assert.NotNull(impl);
            Assert.Equal(1, impl.GetArrayLength());

            bool b = impl[0].TryGetProperty("resolutionKind", out IUntypedObject resolutionKind);
            Assert.True(b);
            Assert.Equal("NumberResolution", resolutionKind.GetString());

            string input = testConnector2._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expectedInput =
@$"POST https://lucgen-apim.azure-api.net/invoke
 authority: lucgen-apim.azure-api.net
 Authorization: Bearer No Auth
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/aaa373836ffd4915bf6eefd63d164adc
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: POST
 x-ms-request-url: /apim/cognitiveservicestextanalytics/16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b/language/:analyze-conversations?api-version=2022-05-01
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""kind"":""Conversation"",""analysisInput"":{{""conversationItem"":{{""id"":""0"",""participantId"":""0"",""language"":""en-us"",""modality"":""text"",""text"":""Book me a flight for Munich""}}}},""parameters"":{{""projectName"":""project1"",""deploymentName"":""deploy1"",""verbose"":true,""stringIndexType"":""TextElement_V8""}}}}
";

            Assert.Equal(expectedInput.Replace("\r\n", "\n").Replace("\r", "\n"), input.Replace("\r\n", "\n").Replace("\r", "\n"));
        }

        [Fact]
        public async Task ACSL_InvokeFunction_v21()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language v2.1.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.PowerFxV1);
            using var httpClient = new HttpClient(testConnector);
            testConnector.SetResponseFromFile(@"Responses\Azure Cognitive Service for Language v2.1_Response.json");

            ConnectorFunction function = OpenApiParser.GetFunctions(apiDoc).OrderBy(cf => cf.Name).ToList()[13];
            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("![kind:s, result:![detectedLanguage:s, prediction:![entities:*[category:s, confidenceScore:w, extraInformation:O, length:w, multipleResolutions:b, offset:w, resolutions:O, text:s, topResolution:O], intents:*[category:s, confidenceScore:w], projectKind:s, topIntent:s], query:s]]", function.ReturnType.ToStringWithDisplayNames());

            RecalcEngine engine = new RecalcEngine(pfxConfig);

            string analysisInput = @"{ conversationItem: { modality: ""text"", language: ""en-us"", text: ""Book me a flight for Munich"" } }";
            string parameters = @"{ deploymentName: ""deploy1"", projectName: ""project1"", verbose: true, stringIndexType: ""TextElement_V8"" }";
            FormulaValue analysisInputParam = engine.Eval(analysisInput);
            FormulaValue parametersParam = engine.Eval(parameters);

            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://lucgen-apim.azure-api.net", "aaa373836ffd4915bf6eefd63d164adc" /* environment Id */, "16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b" /* connectionId */, () => "No Auth", httpClient)
            {
                SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878",
            };

            FormulaValue httpResult = await function.InvokeAync(client, new FormulaValue[] { analysisInputParam, parametersParam }, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(httpResult);
            Assert.True(httpResult is RecordValue);

            RecordValue httpResultValue = (RecordValue)httpResult;
            RecordValue resultValue = (RecordValue)httpResultValue.GetField("result");
            RecordValue predictionValue = (RecordValue)resultValue.GetField("prediction");
            TableValue entitiesValue = (TableValue)predictionValue.GetField("entities");
            IEnumerable<DValue<RecordValue>> rows = entitiesValue.Rows;

            // Get second entity
            RecordValue entityValue2 = rows.Skip(1).First().Value;
            FormulaValue resolutionsValue = entityValue2.GetField("resolutions");

            Assert.True(resolutionsValue is UntypedObjectValue);          
            UOValueVisitor visitor1 = new UOValueVisitor();
            resolutionsValue.Visit(visitor1);

            Assert.Equal("[\"{\\\"resolutionKind\\\":\\\"DateTimeResolution\\\",\\\"value\\\":\\\"2023-02-25\\\"}\"]", visitor1.Result);

            FormulaValue topResolutionValue1 = entityValue2.GetField("topResolution");
            Assert.True(topResolutionValue1 is UntypedObjectValue);

            UOValueVisitor visitor2 = new UOValueVisitor();
            topResolutionValue1.Visit(visitor2);

            Assert.Equal("{\"resolutionKind\":\"DateTimeResolution\",\"value\":\"2023-02-25\"}", visitor2.Result);
        }

        internal class UOValueVisitor : IValueVisitor
        {            
            public string Result { get; private set; }

            public void Visit(BlankValue value)
            {
                Result = string.Empty;
            }

            public void Visit(NumberValue value)
            {
                Result = value.Value.ToString();
            }

            public void Visit(DecimalValue value)
            {
                Result = value.Value.ToString();
            }

            public void Visit(BooleanValue value)
            {
                Result = value.Value.ToString();
            }

            public void Visit(StringValue value)
            {
                Result = value.Value;
            }

            public void Visit(TimeValue value)
            {
                Result = value.Value.ToString();
            }

            public void Visit(DateValue value)
            {
                Result = value.GetConvertedValue(null).ToString();
            }

            public void Visit(DateTimeValue value)
            {
                Result = value.GetConvertedValue(null).ToString();
            }

            public void Visit(ErrorValue value)
            {
                Result = string.Empty;
            }

            public void Visit(RecordValue value)
            {
                var fieldBuilder = new Dictionary<string, string>();

                foreach (var item in value.Fields)
                {
                    item.Value.Visit(this);
                    fieldBuilder.Add(item.Name, Result!);
                }

                Result = JsonConvert.SerializeObject(fieldBuilder);
            }

            public void Visit(TableValue value)
            {
                var rows = new List<string>();

                foreach (var row in value.Rows)
                {
                    row.ToFormulaValue().Visit(this);
                    rows.Add(Result);
                }

                Result = JsonConvert.SerializeObject(rows);
            }

            public void Visit(UntypedObjectValue value)
            {
                Visit(value.Impl);
            }

            public void Visit(OptionSetValue value)
            {
                Result = value.DisplayName;
            }

            public void Visit(ColorValue value)
            {
                Result = string.Empty;
            }

            public void Visit(GuidValue value)
            {
                Result = value.Value.ToString();
            }

            private void Visit(IUntypedObject untypedObject)
            {
                var type = untypedObject.Type;

                if (type is StringType)
                {
                    Result = untypedObject.GetString();
                }
                else if (type is NumberType)
                {
                    Result = untypedObject.GetDouble().ToString();
                }
                else if (type is BooleanType)
                {
                    Result = untypedObject.GetBoolean().ToString();
                }
                else if (type is ExternalType externalType)
                {
                    if (externalType.Kind == ExternalTypeKind.Array)
                    {
                        var rows = new List<string>();                        

                        for (var i = 0; i < untypedObject.GetArrayLength(); i++)
                        {
                            var row = untypedObject[i];
                            Visit(row);
                            rows.Add(Result);
                        }

                        Result = JsonConvert.SerializeObject(rows);
                    }
                    else if (externalType.Kind == ExternalTypeKind.Object)
                    {
                        var fieldBuilder = new Dictionary<string, string>();

                        foreach (var key in new[] { "extraInformationKind", "numberKind", "resolutionKind", "value" })
                        {
                            if (untypedObject.TryGetProperty(key, out var result))
                            {
                                Visit(result);
                                fieldBuilder.Add(key, Result!);
                            }
                        }

                        Result = JsonConvert.SerializeObject(fieldBuilder);
                    }
                    else
                    {
                        Result = string.Empty;
                    }
                }
                else
                {
                    Result = string.Empty;
                }
            }
        }

        [Fact]
        public void LQA_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Language - Question Answering.json");
            List<ServiceFunction> functionList = OpenApiParser.Parse("LQA", doc);
            Assert.Contains(functionList, sf => sf.GetUniqueTexlRuntimeName() == "lQA__GetAnswersFromText");
        }

        [Fact]
        public void SQL_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\SQL Server.json");
            List<ServiceFunction> functionList = OpenApiParser.Parse("SQL", doc);
            Assert.Contains(functionList, sf => sf.GetUniqueTexlRuntimeName() == "sQL__GetProcedureV2");
        }

        [Fact]
        public void Dataverse_Sample()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\DataverseSample.json");
            ConnectorFunction[] functions = OpenApiParser.GetFunctions(doc).ToArray();

            Assert.NotNull(functions);
            Assert.Equal(3, functions.Count());

            Assert.Equal(new List<string>() { "GetLead", "PostLead", "QualifyLead" }, functions.Select(f => f.Name).ToList());
            Assert.Equal(new List<string>() { "GetLead", "PostLead", "QualifyLead" }, functions.Select(f => f.OriginalName).ToList());

            // "x-ms-require-user-confirmation"
            Assert.Equal(new List<bool>() { false, true, true }, functions.Select(f => f.RequiresUserConfirmation).ToList());

            // "x-ms-explicit-input" in "QualifyLead" function parameters       
            Assert.Equal(4, functions[2].RequiredParameters.Length);
            Assert.False(functions[2].RequiredParameters[0].ConnectorType.ExplicitInput); // "leadId"
            Assert.True(functions[2].RequiredParameters[1].ConnectorType.ExplicitInput);  // "CreateAccount"
            Assert.True(functions[2].RequiredParameters[2].ConnectorType.ExplicitInput);  // "CreateContact"
            Assert.True(functions[2].RequiredParameters[3].ConnectorType.ExplicitInput);  // "CreateOpportunity"
            Assert.Single(functions[2].HiddenRequiredParameters);
            Assert.False(functions[2].HiddenRequiredParameters[0].ConnectorType.ExplicitInput); // "Status"
            Assert.Empty(functions[2].OptionalParameters);

            // "enum"
            Assert.Equal(FormulaType.Decimal, functions[1].OptionalParameters[2].ConnectorType.FormulaType); // "leadsourcecode"
            Assert.True(functions[1].OptionalParameters[2].ConnectorType.IsEnum);
            Assert.Equal(Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray(), functions[1].OptionalParameters[2].ConnectorType.EnumValues.Select(fv => (decimal)fv.ToObject()));

            // "x-ms-enum-display-name"
            Assert.NotNull(functions[1].OptionalParameters[2].ConnectorType.EnumDisplayNames);
            Assert.Equal("Advertisement", functions[1].OptionalParameters[2].ConnectorType.EnumDisplayNames[0]);
            Assert.Equal("Employee Referral", functions[1].OptionalParameters[2].ConnectorType.EnumDisplayNames[1]);
            
            Assert.True(functions[1].RequiredParameters[2].ConnectorType.IsEnum); // "msdyn_company@odata.bind"
            Assert.Equal("2b629105-4a26-4607-97a5-0715059e0a55", functions[1].RequiredParameters[2].ConnectorType.EnumValues[0].ToObject());
            Assert.Equal("5cacddd3-d47f-4023-a68e-0ce3e0d401fb", functions[1].RequiredParameters[2].ConnectorType.EnumValues[1].ToObject());
            Assert.Equal("INMF", functions[1].RequiredParameters[2].ConnectorType.EnumDisplayNames[0]);
            Assert.Equal("MYMF", functions[1].RequiredParameters[2].ConnectorType.EnumDisplayNames[1]);

            OptionSet os1 = functions[1].RequiredParameters[2].ConnectorType.OptionSet;

            Assert.NotNull(os1);
            Assert.Equal("msdyn_company@odata.bind", os1.EntityName);
            Assert.Equal("msdyn_company@odata.bind", os1.FormulaType.OptionSetName);
        }
    }

    public static class Extensions
    {
        public static RecordType MakeRecordType(params (string, FormulaType)[] columns)
        {
            RecordType rt = RecordType.Empty();

            foreach ((string name, FormulaType type) in columns)
            {
                rt = rt.Add(name, type);
            }

            return rt;
        }

        public static TableType MakeTableType(params (string, FormulaType)[] columns)
        {
            TableType tt = TableType.Empty();

            foreach ((string name, FormulaType type) in columns)
            {
                tt = tt.Add(name, type);
            }

            return tt;
        }

        public static OptionSetValueType MakeOptionSetType(string name, params string[] names)
        {
            return MakeOptionSet(name, names).FormulaType;
        }

        public static OptionSet MakeOptionSet(string name, params string[] names)
        {
            return new OptionSet(name, DisplayNameUtility.MakeUnique(names.ToDictionary(n => n, n => n)));
        }
    }
}
