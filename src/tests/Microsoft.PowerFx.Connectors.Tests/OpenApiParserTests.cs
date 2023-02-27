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
                                                            ("confidenceScore", FormulaType.Number),
                                                            ("extraInformation", FormulaType.UntypedObject), // property has a discriminator
                                                            ("length", FormulaType.Number),
                                                            ("offset", FormulaType.Number),
                                                            ("resolutions", FormulaType.UntypedObject),      // property has a discriminator
                                                            ("text", FormulaType.String))),
                                                        ("intents", Extensions.MakeTableType(
                                                            ("category", FormulaType.String),
                                                            ("confidenceScore", FormulaType.Number))),
                                                        ("projectKind", FormulaType.String),
                                                        ("topIntent", FormulaType.String))),
                                                        ("query", FormulaType.String))));

            string rt3Name = expectedReturnType.ToStringWithDisplayNames();
            string returnTypeName = expectedReturnType.ToStringWithDisplayNames();

            Assert.Equal(rt3Name, returnTypeName);
            Assert.True((FormulaType)expectedReturnType == returnType);
        }
#pragma warning restore SA1118, SA1137

        [Fact]
        public async Task ACSL_InvokeFunction()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Azure Cognitive Service for Language.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig pfxConfig = new PowerFxConfig(Features.All);
            using var httpClient = new HttpClient(testConnector);
            testConnector.SetResponseFromFile(@"Responses\Azure Cognitive Service for Language_Response.json");

            ConnectorFunction function = OpenApiParser.GetFunctions(apiDoc).OrderBy(cf => cf.Name).ToList()[19];
            Assert.Equal("ConversationAnalysisAnalyzeConversationConversation", function.Name);
            Assert.Equal("![kind:s, result:![detectedLanguage:s, prediction:![entities:*[category:s, confidenceScore:n, extraInformation:O, length:n, offset:n, resolutions:O, text:s], intents:*[category:s, confidenceScore:n], projectKind:s, topIntent:s], query:s]]", function.ReturnType.ToStringWithDisplayNames());

            RecalcEngine engine = new RecalcEngine(pfxConfig);

            string analysisInput = @"{ conversationItem: { modality: ""text"", language: ""en-us"", text: ""Book me a flight for Munich"" } }";
            string parameters = @"{ deploymentName: ""deploy1"", projectName: ""project1"", verbose: true, stringIndexType: ""TextElement_V8"" }";
            FormulaValue analysisInputParam = engine.Eval(analysisInput);
            FormulaValue parametersParam = engine.Eval(parameters);

            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://lucgen-apim.azure-api.net", "aaa373836ffd4915bf6eefd63d164adc" /* environment Id */, "16e7c181-2f8d-4cae-b1f0-179c5c4e4d8b" /* connectionId */, () => "No Auth", httpClient)
            { 
                SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878",                
            };

            FormulaValue httpResult = await function.InvokeAync(client, new FormulaValue[] { analysisInputParam, parametersParam }, CancellationToken.None);

            Assert.NotNull(httpResult);
            Assert.True(httpResult is RecordValue);
            
            RecordValue httpResultValue = (RecordValue)httpResult;
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
          
            string input = testConnector._log.ToString();            
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

            Assert.Equal(expectedInput, input);
        }

        [Fact]

        public void LQA_Load()
        {
            OpenApiDocument doc = Helpers.ReadSwagger(@"Swagger\Language - Question Answering.json");
            List<ServiceFunction> functionList = OpenApiParser.Parse("LQA", doc);
            Assert.Contains(functionList, sf => sf.GetUniqueTexlRuntimeName() == "lQA__GetAnswersFromText");
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
