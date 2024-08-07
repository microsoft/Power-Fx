// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class OpenAITests
    {
        private readonly ITestOutputHelper _output;

        public OpenAITests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task OpenAI_CreateImage()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\OpenAI.yaml", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"sw...";
            using HttpClient httpClient = new TestHttpClient(token, new HttpClient(testConnector));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("OpenAI"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("OpenAI", httpClient, console: _output));

            testConnector.SetResponseFromFile(@"Responses\OpenAI CreateImage.json");
            FormulaValue fv = await engine.EvalAsync(@"First(OpenAI.createImage(""A cute owl in cartoon form"",{ n: 2 }).data).url", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("https://oaidalleapiprodscus.blob.core.windows.net/private/org-lAPlc1If/user-1ps/img-HYvauU1v8gutNO7m4HmA.png?st=2024-06-17T13%3A35%3A17Z&se=2024-06-17T15%3A35%3A17Z&sp=r&sv=2023-11-03&sr=b&rscd=inline&rsct=image/png&skoid=6aaadede-4fb3-4698-a8f6-684d7786b067&sktid=a48cca56-e6da-484e-a814-9c849652bcb3&skt=2024-06-17T06%3A40%3A52Z&ske=2024-06-18T06%3A40%3A52Z&sks=b&skv=2023-11-03&sig=arl0jgu2YkXX", sv.Value);
        }

        [Fact]
        public async Task OpenAI_CreateImageEdit()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\OpenAI.yaml", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"sw...";

            using HttpClient httpClient = new TestHttpClient(token, new HttpClient(testConnector));

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("owl", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, GetBlobFromFile("Owl.png", false));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("OpenAI"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("OpenAI", httpClient, console: _output));

            testConnector.SetResponseFromFile(@"Responses\OpenAI CreateImageEdit.json");
            FormulaValue fv = await engine.EvalAsync(@"First(OpenAI.createImageEdit(owl, ""Owl with lovely eyes"").data).url", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("https://oaidalleapiprodscus.blob.core.windows.net/private/org-TpmJMlAPlc1If/user-1psBzprTS/img-wa3svgC9yasf3TN8UTUuDTSt.png?st=2024-06-18T06%3A35%3A54Z&se=2024-06-18T08%3A35%3A54Z&sp=r&sv=2023-11-03&sr=b&rscd=inline&rsct=image/png&skoid=6aaadede-4fb3-4698-a8f6-684d7786b067&sktid=a48cca56-e6da-484e-a814-9c849652bcb3&skt=2024-06-17T16%3A09%3A31Z&ske=2024-06-18T16%3A09%3A31Z&sks=b&skv=2023-11-03&sig=PVodekbPhjaTeXX", sv.Value);
        }

        [Fact]
        public async Task OpenAI_CreateImageEditWithMask()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\OpenAI.yaml", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"sw...";

            using HttpClient httpClient = new TestHttpClient(token, new HttpClient(testConnector));

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("owl", FormulaType.Blob);
            ISymbolSlot slot2 = symbolTable.AddVariable("mask", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, GetBlobFromFile("Owl.NoAlpha.png", false));
            symbolValues.Set(slot2, GetBlobFromFile("Mask.png", false));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("OpenAI"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("OpenAI", httpClient, console: _output));

            testConnector.SetResponseFromFile(@"Responses\OpenAI CreateImageEditWithMask.json");
            FormulaValue fv = await engine.EvalAsync(@"First(OpenAI.createImageEdit(owl, ""Owl with red eyes"", {mask: mask}).data).url", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("https://oaidalleapiprodscus.blob.core.windows.net/private/org-TpmJMlAPlc1If/user-1psBzprTS/img-VqWfhz6PwSzu2lU3Yu4DP82F.png?st=2024-06-18T06%3A38%3A35Z&se=2024-06-18T08%3A38%3A35Z&sp=r&sv=2023-11-03&sr=b&rscd=inline&rsct=image/png&skoid=6aaadede-4fb3-4698-a8f6-684d7786b067&sktid=a48cca56-e6da-484e-a814-9c849652bcb3&skt=2024-06-17T19%3A20%3A08Z&ske=2024-06-18T19%3A20%3A08Z&sks=b&skv=2023-11-03&sig=qUeql9LRRsNLEV9/ALrXX", sv.Value);
        }

        [Fact]
        public async Task OpenAI_CreateImageVariation()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\OpenAI.yaml", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"sw...";

            using HttpClient httpClient = new TestHttpClient(token, new HttpClient(testConnector));

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("owl", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, GetBlobFromFile("Owl.png", false));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("OpenAI"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("OpenAI", httpClient, console: _output));

            testConnector.SetResponseFromFile(@"Responses\OpenAI CreateImageVariation.json");
            FormulaValue fv = await engine.EvalAsync(@"First(OpenAI.createImageVariation(owl, {n: 3}).data).url", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("https://oaidalleapiprodscus.blob.core.windows.net/private/org-TpmJMlAPlc1If/user-1psBzprTS/img-0pFVABRSmpr6vm5JOEGoMkEc.png?st=2024-06-18T06%3A40%3A35Z&se=2024-06-18T08%3A40%3A35Z&sp=r&sv=2023-11-03&sr=b&rscd=inline&rsct=image/png&skoid=6aaadede-4fb3-4698-a8f6-684d7786b067&sktid=a48cca56-e6da-484e-a814-9c849652bcb3&skt=2024-06-17T16%3A01%3A40Z&ske=2024-06-18T16%3A01%3A40Z&sks=b&skv=2023-11-03&sig=rhjK96adeCFr2DwXX", sv.Value);
        }

        [Fact]
        public async Task OpenAI_CreateImageVariation2()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\OpenAI.yaml", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"sw...";

            using HttpClient httpClient = new TestHttpClient(token, new HttpClient(testConnector));

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("owl", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, GetBlobFromFile("Owl.png", false));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("OpenAI"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("OpenAI", httpClient, console: _output));

            testConnector.SetResponseFromFile(@"Responses\OpenAI CreateImageVariation2.json");
            FormulaValue fv = await engine.EvalAsync(@"First(OpenAI.createImageVariation(owl, {n: 3, response_format: ""b64_json"", size: ""256x256""}).data).b64_json", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.StartsWith("iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAIAAADTED8xAAAAbGVYSWZNTQAqAAAACAACknwAAgAAAC0AAAAmkoYAAgAAABgAAABUAAAAAE9wZW5BSS0tcmVx", sv.Value);
        }

        private static BlobValue GetBlobFromFile(string file, bool base64)
        {
            byte[] bytes = File.ReadAllBytes(file);
            return new BlobValue(new ByteArrayBlob(bytes));
        }
    }

    public class TestHttpClient : HttpClient
    {
        private readonly HttpMessageInvoker _client;
        private readonly string _token;

        public TestHttpClient(string token, HttpMessageInvoker client)
            : base()
        {
            _token = token;
            _client = client;
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using HttpRequestMessage req = await AddAuthentication(request).ConfigureAwait(false);
            return await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpRequestMessage> AddAuthentication(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", "Bearer " + _token);
            return request;
        }
    }
}
