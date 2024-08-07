// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class PowerPlatformBlobTests
    {
        private readonly ITestOutputHelper _output;

        public PowerPlatformBlobTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Blob_Download()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\TestConnector05.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOi...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://7526ddf1-6e97-eed6-86bb-8fd46790d670.05.custom.tip1002.azure-apihub.net", "7526ddf1-6e97-eed6-86bb-8fd46790d670" /* env */, "ed72cbf9f068460593820086df5640fa" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("Test05"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Test05", ppClient, console: _output));

            testConnector.SetResponse(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());
            FormulaValue fv = await engine.EvalAsync(@"Test05.Download()", CancellationToken.None, runtimeConfig: runtimeConfig);
            BlobValue bv = Assert.IsType<BlobValue>(fv);

            string str = await bv.GetAsStringAsync(Encoding.UTF8, CancellationToken.None);
            Assert.Equal(100, str.Length);
        }

#if false
        // excluding tests as this test connector returns text/plain which we don't manage properly for now

        [Fact]
        public async Task Blob_Upload()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\TestConnector05.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOiJ...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://7526ddf1-6e97-eed6-86bb-8fd46790d670.05.custom.tip1002.azure-apihub.net", "7526ddf1-6e97-eed6-86bb-8fd46790d670" /* env */, "ed72cbf9f068460593820086df5640fa" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("file", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, new BlobValue(new ByteArrayBlob(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray())));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("Test05"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("Test05", ppClient, console: _output));

            testConnector.SetResponse("Received 100 bytes for file file with fileName file", contentType: "text/plain");
            FormulaValue fv = await engine.EvalAsync(@"Test05.Upload({clientDate:Now(), description: ""File"", file: file})", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("Received 100 bytes for file file with fileName file", sv.Value);
        }

        [Fact]
        public async Task Blob_Upload64()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\TestConnector05.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOiJK...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://7526ddf1-6e97-eed6-86bb-8fd46790d670.05.custom.tip1002.azure-apihub.net", "7526ddf1-6e97-eed6-86bb-8fd46790d670" /* env */, "ed72cbf9f068460593820086df5640fa" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot slot = symbolTable.AddVariable("file", FormulaType.Blob);
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(slot, new BlobValue(new Base64Blob("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiYw==")));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("Test05"), apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues).AddRuntimeContext(new TestConnectorRuntimeContext("Test05", ppClient, console: _output));

            testConnector.SetResponse("Received 100 bytes for file file with fileName file", contentType: "text/plain");
            FormulaValue fv = await engine.EvalAsync(@"Test05.Upload({clientDate:Now(), description: ""File"", file: file})", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            StringValue sv = Assert.IsType<StringValue>(fv);
            Assert.Equal("Received 100 bytes for file file with fileName file", sv.Value);
        }
#endif
    }
}
