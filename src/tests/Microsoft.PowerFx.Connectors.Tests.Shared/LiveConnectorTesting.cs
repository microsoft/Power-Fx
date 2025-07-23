// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Tests.LanguageServiceProtocol;
using Microsoft.PowerFx.Types;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests.Shared
{
    public class LiveConnectorTesting
    {
        private readonly ITestOutputHelper _output;

        public LiveConnectorTesting(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestLiveConnectorAsync()
        {
            var swaggerFileName = "C:\\Jas\\Power-FX\\src\\tests\\Microsoft.PowerFx.Connectors.Tests.Shared\\Swagger\\testSwagger.json";
            var nameSpace = "TestNamespace";
            var envId = "2e60e1f8-dcfd-e26e-ad11-76bce40da16b";
            var connectionId = "6b29d48fa9ea4208908d290f95d3f3cc";
            var userAgent = "UserAgent";
            var bToken = "";
            var engine = new RecalcEngine();
            OpenApiDocument doc = LoadOpenApiFromFile(swaggerFileName);
            var connectorSettings = new ConnectorSettings(nameSpace);

            // var client = new PowerPlatformConnectorClient(doc, envId, connectionId, () => bToken, new HttpClient());

            using var client = new PowerPlatformConnectorClient2(doc, envId, connectionId, userAgent, async (cancellationToken) => bToken, new HttpClientHandler());

            using var invoker = new HttpMessageInvoker(client);

            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext(nameSpace, invoker, console: _output));
            engine.Config.AddActionConnector(connectorSettings, doc);
            var check = engine.Check($"{nameSpace}.CurrentWeather(\"Seattle\",\"C\").responses.weather.current.temp");

            Assert.True(check.IsSuccess);

            var result = await check.GetEvaluator().EvalAsync(cancellationToken: CancellationToken.None, runtimeConfig);

            Assert.IsAssignableFrom<FormulaValue>(result);
            Assert.IsNotAssignableFrom<ErrorValue>(result);
        }

        /// <summary>
        /// Reads a JSON file, grabs properties.swagger, and turns it into an OpenApiDocument.
        /// </summary>
        public static OpenApiDocument LoadOpenApiFromFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var doc = JsonDocument.Parse(fs);

            if (!doc.RootElement.TryGetProperty("properties", out var props) ||
                !props.TryGetProperty("swagger", out var swaggerElement))
            {
                throw new InvalidOperationException(
                    "The JSON does not contain properties.swagger.");
            }

            string swaggerJson = swaggerElement.GetRawText();

            var reader = new OpenApiStringReader();
            var openApi = reader.Read(swaggerJson, out var diagnostic);

            if (diagnostic?.Errors?.Count > 0)
            {
                throw new InvalidOperationException(
                    "Swagger parsing produced errors: " +
                    string.Join("; ", diagnostic.Errors));
            }

            return openApi;
        }
    }
}
