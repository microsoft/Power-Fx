// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions("ACSL", doc).OrderBy(cf => cf.Name).ToList();
            Assert.Equal(51, functions.Count);
            ConnectorFunction conversationAnalysisAnalyzeConversationConversation = functions[19];

            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", conversationAnalysisAnalyzeConversationConversation.Name);
            Assert.Equal("Analyzes the input conversation utterance.", conversationAnalysisAnalyzeConversationConversation.Description);
            Assert.Equal("Conversations (CLU) (2022-05-01)", conversationAnalysisAnalyzeConversationConversation.Summary);
            Assert.Equal("/apim/cognitiveservicestextanalytics/{connectionId}/language/:analyze-conversations", conversationAnalysisAnalyzeConversationConversation.OperationPath);
            Assert.Equal(HttpMethod.Post, conversationAnalysisAnalyzeConversationConversation.HttpMethod);
        }

        [Fact]
        public void ACSL_GetFunctionNames22()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language v2.2.json");               
            List<ConnectorFunction> functions = OpenApiParser.GetFunctions("ACSL", doc).OrderBy(cf => cf.Name).ToList();
            ConnectorFunction detectSentimentV3 = functions.First(cf => cf.Name == "DetectSentimentV3");

            Assert.Equal("Documents", detectSentimentV3.OptionalParameters[0].Summary);
            Assert.Equal("The document to analyze.", detectSentimentV3.OptionalParameters[0].Description);
        }

        [Fact]
        public void ACSL_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language.json");
            (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) = OpenApiParser.Parse(new ConnectorSettings("ACSL"), doc);
            Assert.Contains(connectorFunctions, func => func.Namespace == "ACSL" && func.Name == "ConversationAnalysisAnalyzeConversationConversation");
            Assert.Contains(texlFunctions, func => func.Namespace.Name.Value == "ACSL" && func.Name == "ConversationAnalysisAnalyzeConversationConversation");
        }

#pragma warning disable SA1118, SA1117, SA1119, SA1137

        [Fact]
        public void ACSL_GetFunctionParameters()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Azure Cognitive Service for Language.json");
            ConnectorFunction function = OpenApiParser.GetFunctions("ACSL", doc).OrderBy(cf => cf.Name).ToList()[19];

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

        [Fact]
        public async Task ACSL_InvokeFunction()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.PowerFxV1);
            ConnectorFunction function = OpenApiParser.GetFunctions("ACSL", apiDoc).OrderBy(cf => cf.Name).ToList()[19];
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

            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("ACSL", client);

            FormulaValue httpResult = await function.InvokeAsync(new FormulaValue[] { analysisInputParam, parametersParam }, context, CancellationToken.None).ConfigureAwait(false);
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

            BaseRuntimeConnectorContext context2 = new TestConnectorRuntimeContext("ACSL", client2);

            FormulaValue httpResult2 = await function.InvokeAsync(new FormulaValue[] { analysisInputParam, parametersParam }, context2, CancellationToken.None).ConfigureAwait(false);

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
        public async Task AzureOpenAiGetFunctions()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Open AI.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.PowerFxV1);
            ConnectorFunction[] functions = OpenApiParser.GetFunctions("OpenAI", apiDoc).OrderBy(cf => cf.Name).ToArray();

            Assert.Equal("ChatCompletionsCreate", functions[0].Name);
            Assert.Equal("![choices:*[finish_reason:s, index:w, message:![content:s, role:s]], created:w, id:s, model:s, object:s, usage:![completion_tokens:w, prompt_tokens:w, total_tokens:w]]", functions[0].ReturnType.ToStringWithDisplayNames());

            Assert.Equal("CompletionsCreate", functions[1].Name);
            Assert.Equal("![choices:*[finish_reason:s, index:w, logprobs:![text_offset:*[Value:w], token_logprobs:*[Value:w], tokens:*[Value:s], top_logprobs:*[]], text:s], created:w, id:s, model:s, object:s, usage:![completion_tokens:w, prompt_tokens:w, total_tokens:w]]", functions[1].ReturnType.ToStringWithDisplayNames());
            
            Assert.Equal("ExtensionsChatCompletionsCreate", functions[2].Name);
            Assert.Equal("![choices:*[content_filter_results:![error:![code:s, message:s], hate:![filtered:b, severity:s], self_harm:![filtered:b, severity:s], sexual:![filtered:b, severity:s], violence:![filtered:b, severity:s]], finish_reason:s, index:w, messages:*[content:s, end_turn:b, index:w, recipient:s, role:s]], created:w, id:s, model:s, object:s, prompt_filter_results:*[content_filter_results:![error:![code:s, message:s], hate:![filtered:b, severity:s], self_harm:![filtered:b, severity:s], sexual:![filtered:b, severity:s], violence:![filtered:b, severity:s]], prompt_index:w], usage:![completion_tokens:w, prompt_tokens:w, total_tokens:w]]", functions[2].ReturnType.ToStringWithDisplayNames());
        }

        [Fact]
        public async Task ACSL_InvokeFunction_v21()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language v2.1.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.PowerFxV1);
            using var httpClient = new HttpClient(testConnector);
            testConnector.SetResponseFromFile(@"Responses\Azure Cognitive Service for Language v2.1_Response.json");

            ConnectorFunction function = OpenApiParser.GetFunctions("ACSL", apiDoc).OrderBy(cf => cf.Name).ToList()[13];
            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("![kind:s, result:![detectedLanguage:s, prediction:![entities:*[category:s, confidenceScore:w, extraInformation:O, length:w, multipleResolutions:b, offset:w, resolutions:O, text:s, topResolution:O], intents:*[category:s, confidenceScore:w], projectKind:s, topIntent:s], query:s]]", function.ReturnType.ToStringWithDisplayNames());

            RecalcEngine engine = new RecalcEngine(pfxConfig);            

            string analysisInput = @"{ conversationItem: { modality: ""text"", language: ""en-us"", text: ""Book me a flight for Munich"" } }";
            string parameters = @"{ deploymentName: ""deploy1"", projectName: ""project1"", verbose: true, stringIndexType: ""TextElement_V8"" }";
            FormulaValue analysisInputParam = engine.Eval(analysisInput);
            FormulaValue parametersParam = engine.Eval(parameters);

            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://lucgen-apim.azure-api.net", "aaa373836ffd4915bf6eefd63d164adc" /* environment Id */, "16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b" /* connectionId */, () => "No Auth", httpClient) { SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878", };
            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("ACSL", client);
            
            FormulaValue httpResult = await function.InvokeAsync(new FormulaValue[] { analysisInputParam, parametersParam }, context, CancellationToken.None).ConfigureAwait(false);

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
            (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) = OpenApiParser.Parse(new ConnectorSettings("LQA"), doc);
            Assert.Contains(texlFunctions, func => func.Namespace.Name.Value == "LQA" && func.Name == "GetAnswersFromText");
        }

        [Fact]
        public void SQL_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\SQL Server.json");
            (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) = OpenApiParser.Parse(new ConnectorSettings("SQL"), doc);
            Assert.Contains(texlFunctions, func => func.Namespace.Name.Value == "SQL" && func.Name == "GetProcedureV2");
        }

        [Fact]
        public void Dataverse_Sample()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\DataverseSample.json");
            ConnectorFunction[] functions = OpenApiParser.GetFunctions("DV", doc).ToArray();

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

            // "x-ms-visibility"
            (0..3).ForAll(i => Assert.Equal(Visibility.None, functions[2].RequiredParameters[i].ConnectorType.Visibility));
            Assert.Equal(Visibility.Internal, functions[2].HiddenRequiredParameters[0].ConnectorType.Visibility); // "Status"
            
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

        [Fact]
        public void VisibilityTest()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\AzureBlobStorage.json");
            ConnectorFunction[] functions = OpenApiParser.GetFunctions("AzBlob", doc).ToArray();

            ConnectorFunction createFileV2 = functions.First(f => f.Name == "CreateFileV2");

            Assert.Equal(4, createFileV2.RequiredParameters.Length);
            Assert.Equal(3, createFileV2.OptionalParameters.Length);
            Assert.Empty(createFileV2.HiddenRequiredParameters);

            Assert.Equal("important", createFileV2.Visibility);

            Assert.Equal("dataset", createFileV2.RequiredParameters[0].Name);
            Assert.Equal("folderPath", createFileV2.RequiredParameters[1].Name);
            Assert.Equal("name", createFileV2.RequiredParameters[2].Name);
            Assert.Equal("body", createFileV2.RequiredParameters[3].Name);
            (0..3).ForAll(i => Assert.Equal(Visibility.None, createFileV2.RequiredParameters[i].ConnectorType.Visibility));

            Assert.Equal("queryParametersSingleEncoded", createFileV2.OptionalParameters[0].Name);
            Assert.Equal("Content-Type", createFileV2.OptionalParameters[1].Name);
            Assert.Equal("ReadFileMetadataFromServer", createFileV2.OptionalParameters[2].Name);
            Assert.Equal(Visibility.Internal, createFileV2.OptionalParameters[0].ConnectorType.Visibility);
            Assert.Equal(Visibility.Advanced, createFileV2.OptionalParameters[1].ConnectorType.Visibility);
            Assert.Equal(Visibility.Internal, createFileV2.OptionalParameters[2].ConnectorType.Visibility);

            Assert.Equal(Visibility.None, createFileV2.ConnectorReturnType.Visibility);

            ConnectorFunction listFolderV4 = functions.First(f => f.Name == "ListFolderV4");

            Assert.Equal(Visibility.None, listFolderV4.ConnectorReturnType.Visibility);
            Assert.Equal(Visibility.None, listFolderV4.ConnectorReturnType.Fields[0].Visibility);
            Assert.Equal(Visibility.Advanced, listFolderV4.ConnectorReturnType.Fields[1].Visibility);
            Assert.Equal(Visibility.Advanced, listFolderV4.ConnectorReturnType.Fields[2].Visibility);
        }

        [Fact]
        public void DynamicReturnValueTest()
        {
            using HttpClient httpClient = new ();
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\SQL Server.json");
            ConnectorFunction[] functions = OpenApiParser.GetFunctions("SQL", doc).ToArray();

            ConnectorFunction createFileV2 = functions.First(f => f.Name == "ExecuteProcedureV2");

            Assert.Equal(4, createFileV2.RequiredParameters.Length);
            Assert.Empty(createFileV2.OptionalParameters);
            Assert.Empty(createFileV2.HiddenRequiredParameters);

            Assert.NotNull(createFileV2.DynamicReturnSchema);
            Assert.Null(createFileV2.DynamicReturnProperty);

            Assert.Equal("GetProcedureV2", createFileV2.DynamicReturnSchema.OperationId);
            Assert.NotNull(createFileV2.DynamicReturnSchema.ConnectorFunction);
            Assert.Equal("GetProcedureV2", createFileV2.DynamicReturnSchema.ConnectorFunction.Name);
            Assert.Equal("schema/procedureresultschema", createFileV2.DynamicReturnSchema.ValuePath);
            Assert.Equal(3, createFileV2.DynamicReturnSchema.ParameterMap.Count);

            Assert.True(createFileV2.DynamicReturnSchema.ParameterMap["server"] is DynamicConnectorExtensionValue dv1 && dv1.Reference == "server");
            Assert.True(createFileV2.DynamicReturnSchema.ParameterMap["database"] is DynamicConnectorExtensionValue dv2 && dv2.Reference == "database");
            Assert.True(createFileV2.DynamicReturnSchema.ParameterMap["procedure"] is DynamicConnectorExtensionValue dv3 && dv3.Reference == "procedure");

            ConnectorFunction executePassThroughNativeQueryV2 = functions.First(f => f.Name == "ExecutePassThroughNativeQueryV2");

            Assert.Equal(2, executePassThroughNativeQueryV2.RequiredParameters.Length);
            Assert.Equal(3, executePassThroughNativeQueryV2.OptionalParameters.Length);
            Assert.Empty(executePassThroughNativeQueryV2.HiddenRequiredParameters);

            Assert.NotNull(executePassThroughNativeQueryV2.DynamicReturnSchema);
            Assert.NotNull(executePassThroughNativeQueryV2.DynamicReturnProperty);

            Assert.Equal("GetPassThroughNativeQueryMetadataV2", executePassThroughNativeQueryV2.DynamicReturnSchema.OperationId);
            Assert.NotNull(executePassThroughNativeQueryV2.DynamicReturnSchema.ConnectorFunction);
            Assert.Equal("GetPassThroughNativeQueryMetadataV2", executePassThroughNativeQueryV2.DynamicReturnSchema.ConnectorFunction.Name);
            Assert.Equal("schema/queryresults", executePassThroughNativeQueryV2.DynamicReturnSchema.ValuePath);
            Assert.Equal(4, executePassThroughNativeQueryV2.DynamicReturnSchema.ParameterMap.Count);
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnSchema.ParameterMap["server"] is DynamicConnectorExtensionValue dv4 && dv4.Reference == "server");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnSchema.ParameterMap["database"] is DynamicConnectorExtensionValue dv5 && dv5.Reference == "database");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnSchema.ParameterMap["query"] is DynamicConnectorExtensionValue dv6 && dv6.Reference == "query");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnSchema.ParameterMap["formalParameters"] is DynamicConnectorExtensionValue dv7 && dv7.Reference == "formalParameters");
            
            Assert.Equal("GetPassThroughNativeQueryMetadataV2", executePassThroughNativeQueryV2.DynamicReturnProperty.OperationId);
            Assert.NotNull(executePassThroughNativeQueryV2.DynamicReturnProperty.ConnectorFunction);
            Assert.Equal("GetPassThroughNativeQueryMetadataV2", executePassThroughNativeQueryV2.DynamicReturnProperty.ConnectorFunction.Name);
            Assert.Equal("schema/queryresults", executePassThroughNativeQueryV2.DynamicReturnProperty.ItemValuePath);
            Assert.Equal(4, executePassThroughNativeQueryV2.DynamicReturnProperty.ParameterMap.Count);
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnProperty.ParameterMap["server"] is DynamicConnectorExtensionValue dv8 && dv8.Reference == "server");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnProperty.ParameterMap["database"] is DynamicConnectorExtensionValue dv9 && dv9.Reference == "database");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnProperty.ParameterMap["query/query"] is DynamicConnectorExtensionValue dv10 && dv10.Reference == "query/query");
            Assert.True(executePassThroughNativeQueryV2.DynamicReturnProperty.ParameterMap["query/formalParameters"] is DynamicConnectorExtensionValue dv11 && dv11.Reference == "query/formalParameters");
        }

        [Fact]
        public async Task DirectIntellisenseTest()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            using var httpClient = new HttpClient(testConnector);            
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://tip1002-002.azure-apihub.net", "ddadf2c7-ebdd-ec01-a5d1-502dc07f04b4" /* environment Id */, "4bf9a87fc9054b6db3a4d07a1c1f5a5b" /* connectionId */, () => "eyJ0eXAi...", httpClient) { SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878" };

            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("SQL", client);

            ConnectorFunction[] functions = OpenApiParser.GetFunctions("SQL", testConnector._apiDocument).ToArray();
            ConnectorFunction executeProcedureV2 = functions.First(f => f.Name == "ExecuteProcedureV2");

            Assert.True(executeProcedureV2.RequiredParameters[0].SupportsDynamicIntellisense);
            Assert.True(executeProcedureV2.RequiredParameters[1].SupportsDynamicIntellisense);
            Assert.True(executeProcedureV2.RequiredParameters[2].SupportsDynamicIntellisense);
            Assert.True(executeProcedureV2.RequiredParameters[3].SupportsDynamicIntellisense);

            // Keeping only for debugging
            //FormulaValue result = await executeProcedureV2.InvokeAync(client, new FormulaValue[] 
            //{
            //    FormulaValue.New("pfxdev-sql.database.windows.net"),
            //    FormulaValue.New("connectortest"),
            //    FormulaValue.New("sp_1"),
            //    FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue("p1", FormulaValue.New(38)) })
            //}, CancellationToken.None).ConfigureAwait(false);                        

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 3.json");
            ConnectorParameters parameters1 = await executeProcedureV2.GetParameterSuggestionsAsync(
                new NamedValue[] 
                { 
                    new NamedValue("server", FormulaValue.New("pfxdev-sql.database.windows.net")),
                    new NamedValue("database", FormulaValue.New("connectortest"))
                }, 
                "procedure", 
                context, 
                CancellationToken.None).ConfigureAwait(false);

            ConnectorParameterWithSuggestions suggestions1 = parameters1.ParametersWithSuggestions[2];
            Assert.NotNull(suggestions1);
            Assert.NotNull(suggestions1.Suggestions);
            Assert.Equal(2, suggestions1.Suggestions.Count());
            
            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 1.json");
            ConnectorParameters parameters2 = await executeProcedureV2.GetParameterSuggestionsAsync(
                new NamedValue[]
                {
                    new NamedValue("server", FormulaValue.New("pfxdev-sql.database.windows.net")),
                    new NamedValue("database", FormulaValue.New("connectortest")),
                    new NamedValue("procedure", FormulaValue.New("sp_1"))
                },
                "parameters",
                context,
                CancellationToken.None).ConfigureAwait(false);
            
            ConnectorParameterWithSuggestions suggestions2 = parameters2.ParametersWithSuggestions[3];
            Assert.NotNull(suggestions2);
            Assert.NotNull(suggestions2.Suggestions);
            Assert.Single(suggestions2.Suggestions);

            Assert.True(executeProcedureV2.ReturnParameterType.SupportsSuggestions);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 1.json");
            ConnectorType returnType = await executeProcedureV2.GetConnectorReturnSchemaAsync(
                 new NamedValue[]
                {
                    new NamedValue("server", FormulaValue.New("pfxdev-sql.database.windows.net")),
                    new NamedValue("database", FormulaValue.New("connectortest")),
                    new NamedValue("procedure", FormulaValue.New("sp_1"))
                },            
                context,
                CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(returnType);
            Assert.True(returnType.FormulaType is RecordType);

            string input = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            string expected = $@"POST https://tip1002-002.azure-apihub.net/invoke
 authority: tip1002-002.azure-apihub.net
 Authorization: Bearer eyJ0eXAi...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/ddadf2c7-ebdd-ec01-a5d1-502dc07f04b4
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/4bf9a87fc9054b6db3a4d07a1c1f5a5b/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures
 x-ms-user-agent: PowerFx/{version}
POST https://tip1002-002.azure-apihub.net/invoke
 authority: tip1002-002.azure-apihub.net
 Authorization: Bearer eyJ0eXAi...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/ddadf2c7-ebdd-ec01-a5d1-502dc07f04b4
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/4bf9a87fc9054b6db3a4d07a1c1f5a5b/v2/$metadata.json/datasets/pfxdev-sql.database.windows.net,connectortest/procedures/sp_1
 x-ms-user-agent: PowerFx/{version}
POST https://tip1002-002.azure-apihub.net/invoke
 authority: tip1002-002.azure-apihub.net
 Authorization: Bearer eyJ0eXAi...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/ddadf2c7-ebdd-ec01-a5d1-502dc07f04b4
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/4bf9a87fc9054b6db3a4d07a1c1f5a5b/v2/$metadata.json/datasets/pfxdev-sql.database.windows.net,connectortest/procedures/sp_1
 x-ms-user-agent: PowerFx/{version}
";

            Assert.Equal(expected, input);
        }

        [Fact]
        public async Task DataverseTest()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Dataverse.json");
            using var httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://tip1-shared.azure-apim.net", "Default-9f6be790-4a16-4dd6-9850-44a0d2649aef" /* environment Id */, "461a30624723445c9ba87313d8bbefa3" /* connectionId */, () => "eyJ0eXAiO...", httpClient) { SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878" };

            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("DV", client);

            ConnectorFunction[] functions = OpenApiParser.GetFunctions("DV", testConnector._apiDocument).ToArray();
            ConnectorFunction createRecord = functions.First(f => f.Name == "CreateRecordWithOrganization");

            testConnector.SetResponseFromFile(@"Responses\Dataverse_Response_1.json");
            ConnectorParameters parameters1 = await createRecord.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("organization", FormulaValue.New("https://org283e9949.crm10.dynamics.com")) }, "entityName", context, CancellationToken.None).ConfigureAwait(false);
            ConnectorParameterWithSuggestions suggestions1 = parameters1.ParametersWithSuggestions[1];
            Assert.Equal(651, suggestions1.Suggestions.Count);
            Assert.Equal("AAD Users", suggestions1.Suggestions[0].DisplayName);
            Assert.Equal("aadusers", ((StringValue)suggestions1.Suggestions[0].Suggestion).Value);

            testConnector.SetResponseFromFile(@"Responses\Dataverse_Response_2.json");
            ConnectorParameters parameters2 = await createRecord.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("organization", FormulaValue.New("https://org283e9949.crm10.dynamics.com")), new NamedValue("entityName", FormulaValue.New("accounts")) }, "item", context, CancellationToken.None).ConfigureAwait(false);
            ConnectorParameterWithSuggestions suggestions2 = parameters2.ParametersWithSuggestions[2];
            Assert.Equal(119, suggestions2.Suggestions.Count);
            Assert.Equal("accountcategorycode", suggestions2.Suggestions[0].DisplayName);
            Assert.Equal("Decimal", suggestions2.Suggestions[0].Suggestion.Type.ToString());

            string input = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared.azure-apim.net/invoke
 authority: tip1-shared.azure-apim.net
 Authorization: Bearer eyJ0eXAiO...
 organization: https://org283e9949.crm10.dynamics.com
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/Default-9f6be790-4a16-4dd6-9850-44a0d2649aef
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: POST
 x-ms-request-url: /apim/commondataserviceforapps/461a30624723445c9ba87313d8bbefa3/v1.0/$metadata.json/GetEntityListEnum/GetEntitiesWithOrganization
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared.azure-apim.net/invoke
 authority: tip1-shared.azure-apim.net
 Authorization: Bearer eyJ0eXAiO...
 organization: https://org283e9949.crm10.dynamics.com
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/Default-9f6be790-4a16-4dd6-9850-44a0d2649aef
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: GET
 x-ms-request-url: /apim/commondataserviceforapps/461a30624723445c9ba87313d8bbefa3/v1.0/$metadata.json/entities/accounts/postitem
 x-ms-user-agent: PowerFx/{version}
";

            Assert.Equal(expected, input);
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

#pragma warning restore SA1118, SA1117, SA1119, SA1137
}
