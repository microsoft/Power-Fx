﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class LivePublicSwaggerTests
    {
        private readonly ITestOutputHelper _output;

        public LivePublicSwaggerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "These APIs are rate limited and HTTP error 429 is possible")]
        public async Task RealTest()
        {
            var config = new PowerFxConfig();

            // Note that these APIs are rate limited and HTTP error 429 is possible
            var swaggerUrl = "https://api.math.tools/yaml/math.tools.numbers.openapi.yaml";

            // Other intersting files:
            // "https://api.apis.guru/v2/specs/weatherbit.io/2.0.0/swagger.json"
            // "https://www.weatherbit.io/static/swagger.json"

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl, _output);

            // No BaseAdress specified, we'll use the 1st HTTPS one found in the swagger file
            using var client = new HttpClient(); // public auth             
            var funcs = config.AddActionConnector("Math", doc);

            var engine = new RecalcEngine(config);
            var expr = "Math.numberscardinal({number: 1791941})";
            var check = engine.Check(expr);
            var ok = check.IsSuccess;

            Assert.True(ok);

            var runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Math", client));
            FormulaValue result = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail(string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic result2 = result.ToObject();
            string cardinal = result2.contents.cardinal;

            Assert.Equal("one million seven hundred ninety-one thousand nine hundred forty-one", cardinal);
        }

        [Fact(Skip = "These APIs are rate limited and HTTP error 429 is possible")]
        public async Task RealTest2()
        {
            var config = new PowerFxConfig();

            // https://math.tools/api/numbers/
            // Note that these APIs are rate limited and HTTP error 429 is possible
            var swaggerUrl = "https://api.apis.guru/v2/specs/math.tools/1.5/openapi.json";

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl, _output);

            // Here we specify the BaseAddress
            using var client = new HttpClient() { BaseAddress = new Uri("https://api.math.tools") };

            // FailOnUnknownExtension in connectorsettings is set to false (default) as this swagger uses some extensions we don't honnor like x-apisguru-categories, x-origin, x-providerName
            var funcs = config.AddActionConnector(new ConnectorSettings("Math"), doc);

            var engine = new RecalcEngine(config);
            var expr = "Math.numbersbasebinary(632506623)";
            var check = engine.Check(expr);
            var ok = check.IsSuccess;

            Assert.True(ok, string.Join(", ", check.Errors.Select(er => er.Message)));

            var runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Math", client));
            FormulaValue result = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail(string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic dResult = result.ToObject();
            string bin = dResult.contents.answer;

            Assert.Equal("100101101100110100100011111111", bin);

            // Try same number but in base 5 (why not??)
            var expr2 = @"Math.numbersbasebinary(2243410202443, {from: 35})";
            result = await engine.EvalAsync(expr2, CancellationToken.None, runtimeConfig: runtimeConfig);

            if (result is ErrorValue ev2)
            {
                Assert.Fail(string.Join(", ", ev2.Errors.Select(er => er.Message)));
            }

            dynamic dResult2 = result.ToObject();
            string bin2 = dResult.contents.answer;

            Assert.Equal("100101101100110100100011111111", bin2);
        }

        [Fact(Skip = "Live test")]
        public async Task RealTest3()
        {
            var config = new PowerFxConfig();

            // https://date.nager.at/       
            var swaggerUrl = "https://date.nager.at/swagger/v3/swagger.json";

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl, _output);
            using var client = new HttpClient() { BaseAddress = new Uri("https://date.nager.at") };
            var funcs = config.AddActionConnector("Holiday", doc);

            var engine = new RecalcEngine(config);
            var expr = @"Index(Holiday.PublicHolidayPublicHolidaysV3(2023, ""US""), 8)";

            // Validate expression
            var check = engine.Check(expr);

            var ok = check.IsSuccess;
            Assert.True(ok, string.Join(", ", check.Errors.Select(er => er.Message)));

            var runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Holiday", client));
            FormulaValue result = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail(string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic dResult = result.ToObject();
            DateTime independanceDay = dResult.date;
            string independanceDayName = dResult.name;

            Assert.Equal("Independence Day", independanceDayName);
            Assert.Equal(new DateTime(2023, 7, 4), independanceDay);

            expr = @"First(Holiday.PublicHolidayPublicHolidaysV3(2023, ""US"")).localName";
            result = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.Equal("New Year's Day", ((StringValue)result).Value);
        }

        [Fact(Skip = "Live Test")]
        public async Task RealTest4()
        {
            var config = new PowerFxConfig();
            OpenApiDocument docXkcd = await ReadSwaggerFromUrl(@"https://api.apis.guru/v2/specs/xkcd.com/1.0.0/openapi.json", _output);
            OpenApiDocument docWorldTime = await ReadSwaggerFromUrl(@"https://api.apis.guru/v2/specs/worldtimeapi.org/20210108/openapi.json", _output);

            using var clientXkcd = new HttpClient() { BaseAddress = new Uri(@"http://xkcd.com/") };
            using var clientWorldTime = new HttpClient() { BaseAddress = new Uri(@"http://worldtimeapi.org/api/") };
            var funcsXkcd = config.AddActionConnector(new ConnectorSettings("Xkcd"), docXkcd);
            var funcsWorldTime = config.AddActionConnector(new ConnectorSettings("WorldTime"), docWorldTime);

            var engine = new RecalcEngine(config);
            var runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Xkcd", clientXkcd).Add("WorldTime", clientWorldTime));

            FormulaValue fv1 = await engine.EvalAsync(@"Xkcd.comicIdinfo0json(1).transcript", CancellationToken.None, runtimeConfig: runtimeConfig);
            string transcript = ((StringValue)fv1).Value.Replace("\n", "\r\n");
            string expectedTranscript = @"[[A boy sits in a barrel which is floating in an ocean.]]
Boy: I wonder where I'll float next?
[[The barrel drifts into the distance. Nothing else can be seen.]]
{{Alt: Don't we all.}}";
            Assert.Equal(expectedTranscript, transcript);

            FormulaValue fv2 = await engine.EvalAsync(@"First(WorldTime.timezone()).Value", CancellationToken.None, runtimeConfig: runtimeConfig);
            string firstTZ = ((StringValue)fv2).Value;
            Assert.Equal("Africa/Abidjan", firstTZ);
        }

        [Fact(Skip = "Live Test")]
        public async Task FailOnUnknownExtensionTest()
        {
            var config = new PowerFxConfig();
            OpenApiDocument docXkcd = await ReadSwaggerFromUrl(@"https://api.apis.guru/v2/specs/xkcd.com/1.0.0/openapi.json", _output);

            IReadOnlyList<ConnectorFunction> funcsXkcd = config.AddActionConnector(new ConnectorSettings("Xkcd") { FailOnUnknownExtension = true }, docXkcd);

            ConnectorFunction info0json = funcsXkcd.First(f => f.Name == "info0json");
            Assert.False(info0json.IsSupported);
            Assert.Equal("OpenApiDocument Info contains unsupported extensions x-apisguru-categories, x-origin, x-providerName, x-tags, x-unofficialSpec", info0json.NotSupportedReason);

            IReadOnlyList<ConnectorFunction> funcsXkcd2 = config.AddActionConnector(new ConnectorSettings("Xkcd") { FailOnUnknownExtension = false }, docXkcd);

            ConnectorFunction info0json2 = funcsXkcd2.First(f => f.Name == "info0json");
            Assert.True(info0json2.IsSupported);
            Assert.Equal(string.Empty, info0json2.NotSupportedReason);
        }

        [Fact]
        public void DadJokesDirectTest()
        {
            var config = new PowerFxConfig();
            using var testConnector = new LoggingTestServer(@"Swagger\DadJokes.json", _output);
            var apiDoc = testConnector._apiDocument;
        }

        // Get a swagger file from the embedded resources. 
        private static async Task<OpenApiDocument> ReadSwaggerFromUrl(string url, ITestOutputHelper output)
        {
            using HttpClient http = new HttpClient();
            using Stream stream = await http.GetStreamAsync(new Uri(url));         
            OpenApiReaderSettings oars = new OpenApiReaderSettings() { RuleSet = ConnectorFunction.DefaultValidationRuleSet };
            ReadResult rr = await new OpenApiStreamReader(oars).ReadAsync(stream, CancellationToken.None);
            OpenApiDiagnostic diag = rr.OpenApiDiagnostic;
            OpenApiDocument doc = rr.OpenApiDocument;

            if (diag != null && diag.Errors.Count > 0)
            {
                foreach (OpenApiError error in diag.Errors)
                {
                    if (error is OpenApiValidatorError vError)
                    {
                        output.WriteLine($"[OpenApi Error] {vError.RuleName} {vError.Pointer} {vError.Message}");
                    }
                    else
                    {
                        // Could be OpenApiError or OpenApiReferenceError
                        output.WriteLine($"[OpenApi Error] {error.Pointer} {error.Message}");
                    }
                }
            }

            return doc;
        }
    }
}
