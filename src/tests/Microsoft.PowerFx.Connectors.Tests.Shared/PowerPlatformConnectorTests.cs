// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Connectors.Tests;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;
using TestExtensions = Microsoft.PowerFx.Core.Tests.Extensions;

namespace Microsoft.PowerFx.Tests
{
    // Simulate calling PowerPlatform connectors.
    public class PowerPlatformConnectorTests : PowerFxTest
    {
        private readonly ITestOutputHelper _output;

        public PowerPlatformConnectorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // Compare strings, ignoring \r differences that can happen across Operating systems.
        private static void AssertEqual(string expected, string actual)
        {
            Assert.Equal(expected.Replace("\r", string.Empty), actual.Replace("\r", string.Empty));
        }

        // Exercise calling the MSNWeather connector against mocked Swagger and Response.json.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MSNWeatherConnector_CurrentWeather(bool useSwaggerParameter)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = useSwaggerParameter ?
                new PowerPlatformConnectorClient(
                    apiDoc,                                                     // Swagger file
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",                     // environment
                    "shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4",  // connectionId
                    () => "AuthToken1",
                    httpClient)
                {
                    SessionId = "MySessionId"
                }
                : new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",                          // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",                     // environment
                    "shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4",  // connectionId
                    () => "AuthToken1",
                    httpClient)
                {
                    SessionId = "MySessionId"
                };

            var funcs = config.AddActionConnector("MSNWeather", apiDoc, new ConsoleLogger(_output));

            // Function we added where specified in MSNWeather.json
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();

            // "GetMeasureUnits" is internral (x-ms-visibililty is set to internal)
            Assert.Equal(new string[] { "CurrentWeather", "TodaysForecast", "TomorrowsForecast" }, funcNames);

            // Now execute it...
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("MSNWeather", client, console: _output));
            testConnector.SetResponseFromFile(@"Responses\MSNWeather_Response.json");

            var result = await engine.EvalAsync("MSNWeather.CurrentWeather(\"Redmond\", \"Imperial\").responses.weather.current.temp", CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.Equal(53.0m, result.ToObject()); // from response

            // PowerPlatform Connectors transform the request significantly from what was in the swagger.
            // Some of this information comes from setting passed into connector client.
            // Other information is from swagger.
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var expected =
@$"POST https://firstrelease-001.azure-apim.net/invoke
 authority: firstrelease-001.azure-apim.net
 Authorization: Bearer AuthToken1
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/839eace6-59ab-4243-97ec-a5b8fcc104e4
 x-ms-client-session-id: MySessionId
 x-ms-request-method: GET
 x-ms-request-url: /apim/msnweather/shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4/current/Redmond?units=Imperial
 x-ms-user-agent: PowerFx/{version}
";
            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task MSNWeatherConnector_CurrentWeatherOrleans()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-003.azure-apihub.net",                          // endpoint
                    "49970107-0806-e5a7-be5e-7c60e2750f01",                     // environment
                    "480a676ab6e64b168cfa41506014e45d",  // connectionId
                    () => "eyJ0eXAiOiJKV...",
                    httpClient)
            {
                SessionId = "MySessionId"
            };

            var funcs = config.AddActionConnector("MSNWeather", apiDoc, new ConsoleLogger(_output));

            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("MSNWeather", client, console: _output));
            testConnector.SetResponseFromFile(@"Responses\MSNWeather_Response2.json");

            var result = await engine.EvalAsync(@"MSNWeather.CurrentWeather(""Orléans"", ""C"").responses.weather.current.temp", CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.Equal(9m, result.ToObject()); // from response

            // PowerPlatform Connectors transform the request significantly from what was in the swagger.
            // Some of this information comes from setting passed into connector client.
            // Other information is from swagger.
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var expected =
@$"POST https://firstrelease-003.azure-apihub.net/invoke
 authority: firstrelease-003.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/49970107-0806-e5a7-be5e-7c60e2750f01
 x-ms-client-session-id: MySessionId
 x-ms-request-method: GET
 x-ms-request-url: /apim/msnweather/480a676ab6e64b168cfa41506014e45d/current/Orl%c3%a9ans?units=C
 x-ms-user-agent: PowerFx/{version}
";
            AssertEqual(expected, actual);
        }

        [Theory]
        [InlineData(100, null)] // Continue
        [InlineData(200, null)] // Ok
        [InlineData(202, null)] // Accepted
        [InlineData(305, "Use Proxy")]
        [InlineData(411, "Length Required")]
        [InlineData(500, "Internal Server Error")]
        [InlineData(502, "Bad Gateway")]
        public async Task Connector_GenerateErrors(int statusCode, string reasonPhrase)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\TestConnector12.json", _output);
            var apiDoc = testConnector._apiDocument;

            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net", // endpoint
                "839eace6-59ab-4243-97ec-a5b8fcc104e4", // x-ms-client-environment-id
                "8329fe1b70d8494e940a9d3f683e1845", // connectionId
                () => "AuthToken",
                httpClient)
            {
                SessionId = "4851caf7-23ec-43fc-9a56-e1628655a6bd" // from x-ms-request-url
            };

            var funcs = config.AddActionConnector("TestConnector12", apiDoc, new ConsoleLogger(_output));

            // Now execute it...
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("TestConnector12", client, console: _output));

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result = await engine.EvalAsync($"TestConnector12.GenerateError({{error: {statusCode}}})", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.NotNull(result);

            if (statusCode < 300)
            {
                Assert.IsType<DecimalValue>(result);

                var nv = (DecimalValue)result;

                Assert.Equal(statusCode, nv.Value);
            }
            else
            {
                Assert.IsType<ErrorValue>(result);

                var ev = (ErrorValue)result;

                Assert.Equal(FormulaType.Decimal, ev.Type);
                Assert.Single(ev.Errors);

                var err = ev.Errors[0];

                Assert.Equal(ErrorKind.Network, err.Kind);
                Assert.Equal(ErrorSeverity.Critical, err.Severity);
                Assert.Equal($"TestConnector12.GenerateError failed: The server returned an HTTP error with code {statusCode} ({reasonPhrase}). Response: {statusCode}", err.Message);
            }

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result2 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),FirstError.Message)", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.NotNull(result2);
            Assert.IsType<StringValue>(result2);

            var sv2 = (StringValue)result2;

            if (statusCode < 300)
            {
                Assert.Equal(statusCode.ToString(), sv2.Value);
            }
            else
            {
                Assert.Equal($"TestConnector12.GenerateError failed: The server returned an HTTP error with code {statusCode} ({reasonPhrase}). Response: {statusCode}", sv2.Value);
            }

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result3 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),CountRows(AllErrors))", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.NotNull(result3);
            Assert.IsType<StringValue>(result3);

            var sv3 = (StringValue)result3;

            if (statusCode < 300)
            {
                Assert.Equal(statusCode.ToString(), sv3.Value);
            }
            else
            {
                Assert.Equal("1", sv3.Value);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task AzureBlobConnector_UploadFile(bool useSwaggerParameter, bool useHttpsPrefix)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();
            config.AddFunction(new AsBlobFunctionImpl());
            var token = @"eyJ0eXAiO...";

            using var httpClient = new HttpClient(testConnector);
            using var client = useSwaggerParameter ?
                new PowerPlatformConnectorClient(
                    apiDoc,                                 // Swagger file
                    "0a1a8d62-0453-e710-b774-446dc6634a89", // environment
                    "1f18a56da7574c1f8b66a1ea42c23805",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                }
                : new PowerPlatformConnectorClient(
                    (useHttpsPrefix ? "https://" : string.Empty) +
                    "tip2-001.azure-apihub.net",  // endpoint
                    "0a1a8d62-0453-e710-b774-446dc6634a89", // environment
                    "1f18a56da7574c1f8b66a1ea42c23805",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                };

            var funcs = config.AddActionConnector("AzureBlobStorage", apiDoc, new ConsoleLogger(_output));
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();
            Assert.Equal(new string[] { "CopyFile", "CopyFileV2", "CreateBlockBlob", "CreateBlockBlobV2", "CreateFile", "CreateFileV2", "CreateShareLinkByPath", "CreateShareLinkByPathV2", "DeleteFile", "DeleteFileV2", "ExtractFolderV2", "ExtractFolderV3", "GetAccessPolicies", "GetAccessPoliciesV2", "GetFileContent", "GetFileContentByPath", "GetFileContentByPathV2", "GetFileContentV2", "GetFileMetadata", "GetFileMetadataByPath", "GetFileMetadataByPathV2", "GetFileMetadataV2", "ListFolderV2", "ListFolderV4", "ListRootFolderV2", "ListRootFolderV4", "SetBlobTierByPath", "SetBlobTierByPathV2", "UpdateFile", "UpdateFileV2" }, funcNames);

            // Now execute it...
            var engine = new RecalcEngine(config);
            var runtimeContext = new TestConnectorRuntimeContext("AzureBlobStorage", client);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeContext);
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_Response.json");

            // 3rd parameter here expects a blob. It's Base64 value is "YWJj8J+Yig==" and is equivalent to byte[] { 0x61, 0x62, 0x63, 0xF0, 0x9F, 0x98, 0x8A }.
            CheckResult check = engine.Check(@"AzureBlobStorage.CreateFile(""container"", ""01.txt"", AsBlob(""abc😊"")).Size", new ParserOptions() { AllowsSideEffects = true });
            Assert.True(check.IsSuccess);
            _output.WriteLine($"\r\nIR: {check.PrintIR()}");

            var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail($"Error: {string.Join(", ", ev.Errors.Select(er => er.Message))}");
            }

            dynamic res = result.ToObject();
            var size = (double)res;

            Assert.Equal(7.0, size);

            // PowerPlatform Connectors transform the request significantly from what was in the swagger.
            // Some of this information comes from setting passed into connector client.
            // Other information is from swagger.
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var host = useSwaggerParameter ? "localhost:23340" : "tip2-001.azure-apihub.net";
            var expected = @$"POST https://{host}/invoke
 authority: {host}
 Authorization: Bearer {token}
 path: /invoke
 ReadFileMetadataFromServer: True
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/0a1a8d62-0453-e710-b774-446dc6634a89
 x-ms-client-session-id: ccccbff3-9d2c-44b2-bee6-cf24aab10b7e
 x-ms-request-method: POST
 x-ms-request-url: /apim/azureblob/1f18a56da7574c1f8b66a1ea42c23805/datasets/default/files?folderPath=container&name=01.txt&queryParametersSingleEncoded=True
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: text/plain
 [body] abc😊
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task AzureBlobConnector_UploadFileV2()
        {
            string yellowPixel = "/9j/4AAQSkZJRgABAQEAeAB4AAD/2wBDAAIBAQIBAQICAgICAgICAwUDAwMDAwYEBAMFBwYHBwcGBwcICQsJCAgKCAcHCg0KCgsMDAwMBwkODw0MDgsMDAz/2wBDAQICAgMDAwYDAwYMCAcIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD9aKKKK/wTP2g//9k=";

            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();
            config.AddFunction(new AsBlobFunctionImpl());
            var token = @"eyJ0eXAiOiJ...";

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient(
                    "https://tip2-001.azure-apihub.net",  // endpoint
                    "0a1a8d62-0453-e710-b774-446dc6634a89", // environment
                    "6d228109794849049ce8116e5d4ffaf8",     // connectionId
                    () => $"{token}",
                    httpClient)
            {
                SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
            };

            config.AddActionConnector("AzureBlobStorage", apiDoc, new ConsoleLogger(_output));

            var engine = new RecalcEngine(config);
            var runtimeContext = new TestConnectorRuntimeContext("AzureBlobStorage", client);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeContext);

            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_Response2.json");

            CheckResult check = engine.Check($@"AzureBlobStorage.CreateFileV2(""pfxdevstgaccount1"", ""container"", ""001.jpg"", AsBlob(""{yellowPixel}"", true), {{'Content-Type': ""image/jpeg"" }}).Size", new ParserOptions() { AllowsSideEffects = true });
            Assert.True(check.IsSuccess, string.Join(", ", check.Errors.Select(er => er.Message)));
            _output.WriteLine($"\r\nIR: {check.PrintIR()}");

            var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail($"Error: {string.Join(", ", ev.Errors.Select(er => er.Message))}");
            }

            dynamic res = result.ToObject();
            var size = (double)res;

            Assert.Equal(635, size);

            // PowerPlatform Connectors transform the request significantly from what was in the swagger.
            // Some of this information comes from setting passed into connector client.
            // Other information is from swagger.
            var actual = testConnector._log.ToString();

            int idx = actual.IndexOf("[body] ") + 7;
            actual = string.Concat(actual.Substring(0, idx), string.Join(string.Empty, testConnector._log.ToString().Substring(idx, 30).Select((char c) =>
            {
                int d = c;
                return (c < 32 || c > 128) ? $"\\u{d:X4}" : c.ToString();
            })));

            var version = PowerPlatformConnectorClient.Version;
            var host = "tip2-001.azure-apihub.net";
            var expected = @$"POST https://{host}/invoke
 authority: {host}
 Authorization: Bearer {token}
 path: /invoke
 ReadFileMetadataFromServer: True
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/0a1a8d62-0453-e710-b774-446dc6634a89
 x-ms-client-session-id: ccccbff3-9d2c-44b2-bee6-cf24aab10b7e
 x-ms-request-method: POST
 x-ms-request-url: /apim/azureblob/6d228109794849049ce8116e5d4ffaf8/v2/datasets/pfxdevstgaccount1/files?folderPath=container&name=001.jpg&queryParametersSingleEncoded=True
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: image/jpeg
 [body] \uFFFD\uFFFD\uFFFD\uFFFD\u0000\u0010JFIF\u0000\u0001\u0001\u0001\u0000x\u0000x\u0000\u0000\uFFFD\uFFFD\u0000C\u0000\u0002\u0001\u0001\u0002\u0001";

            AssertEqual(expected, actual);
        }

        internal class AsBlobFunctionImpl : BuiltinFunction, IAsyncTexlFunction5
        {
            public AsBlobFunctionImpl()
                : base("AsBlob", (loc) => "Converts a string to a Blob.", FunctionCategories.Text, DType.Blob, 0, 1, 2, DType.String, DType.Boolean)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new TexlStrings.StringGetter[] { (loc) => "string" };
                yield return new TexlStrings.StringGetter[] { (loc) => "string", (loc) => "isBase64Encoded" };
            }

            public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult<FormulaValue>(
                    args[0] is BlankValue || args[0] is BlobValue
                    ? args[0]
                    : args[0] is not StringValue sv
                    ? CommonErrors.RuntimeTypeMismatch(args[0].IRContext)
                    : BlobValue.NewBlob(sv.Value, args.Length >= 2 && args[1] is BooleanValue bv && bv.Value));
            }
        }

        [Fact]
        public async Task AzureBlobConnector_UseOfDeprecatedFunction()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAi...";
            string expr = @"First(azbs.ListRootFolderV3(""pfxdevstgaccount1"")).DisplayName";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "36897fc0-0c0c-eee5-ac94-e12765496c20" /* env */, "d95489a91a5846f4b2c095307d86edd6" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(new ConnectorSettings("azbs") { IncludeInternalFunctions = true }, apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("azbs", ppClient, console: _output));

            CheckResult checkResult = engine.Check("azbs.", symbolTable: null);
            IIntellisenseResult suggestions = engine.Suggest(checkResult, 5);
            List<string> suggestedFuncs = suggestions.Suggestions.Select(s => s.DisplayText.Text).ToList();
            Assert.Equal(15, suggestedFuncs.Count());

            // ListRootFolderV3 is deprecated and should not appear in the list of suggested functions
            Assert.DoesNotContain(suggestedFuncs, str => str == "ListRootFolderV3");
            Assert.Contains(suggestedFuncs, str => str == "ListRootFolderV4");

            // We return a warning message when using a deprecated function
            CheckResult result = engine.Check(expr);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.Equal("In namespace azbs, function ListRootFolderV3 is deprecated.", result.Errors.First().Message);

            // We can still call ListRootFolderV3 deprecated function
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_ListRootFolderV3_response.json");
            FormulaValue fv = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.False(fv is ErrorValue);
            Assert.True(fv is StringValue);

            StringValue sv = (StringValue)fv;
            Assert.Equal("container", sv.Value);
        }

        [Fact]
        public async Task AzureBlobConnector_Paging()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eX...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "36897fc0-0c0c-eee5-ac94-e12765496c20" /* env */, "d95489a91a5846f4b2c095307d86edd6" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            config.AddActionConnector("azbs", apiDoc, new ConsoleLogger(_output));
            config.AddActionConnector(new ConnectorSettings("azbs2") { MaxRows = 7 }, apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("azbs", ppClient, console: _output).Add("azbs2", ppClient));

            testConnector.SetResponseFromFiles(@"Responses\AzureBlobStorage_Paging_Response1.json", @"Responses\AzureBlobStorage_Paging_Response2.json", @"Responses\AzureBlobStorage_Paging_Response3.json");
            FormulaValue fv = await engine.EvalAsync(@"CountRows(azbs.ListFolderV4(""pfxdevstgaccount1"", ""container"").value)", CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.False(fv is ErrorValue);
            Assert.True(fv is DecimalValue);
            Assert.Equal(12m, ((DecimalValue)fv).Value);

            testConnector.SetResponseFromFiles(@"Responses\AzureBlobStorage_Paging_Response1.json", @"Responses\AzureBlobStorage_Paging_Response2.json");
            fv = await engine.EvalAsync(@"CountRows(azbs2.ListFolderV4(""pfxdevstgaccount1"", ""container"").value)", CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.True(fv is DecimalValue);
            Assert.Equal(7m, ((DecimalValue)fv).Value);
        }

        [Fact]
        public async Task AzureBlobConnector_GetFileContentV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();
            config.AddFunction(new AsBlobFunctionImpl());
            var token = @"eyJ0eXAiOi...";

            using var httpClient = new HttpClient(testConnector);
            using var client =
                new PowerPlatformConnectorClient(
                    "firstrelease-003.azure-apihub.net",    // endpoint
                    "49970107-0806-e5a7-be5e-7c60e2750f01", // environment
                    "cf743d059c004f668c6d7eb9e721d0f4",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                };

            config.AddActionConnector("AzureBlobStorage", apiDoc, new ConsoleLogger(_output));

            // Now execute it...
            var engine = new RecalcEngine(config);
            var runtimeContext = new TestConnectorRuntimeContext("AzureBlobStorage", client);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeContext);
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_GetFileContentV2.raw");

            CheckResult check = engine.Check(@"AzureBlobStorage.GetFileContentV2(""pfxdevstgaccount1"", ""JTJmdGVzdCUyZjAxLnR4dA=="")", new ParserOptions() { AllowsSideEffects = true });
            Assert.True(check.IsSuccess, string.Join(", ", check.Errors.Select(er => er.Message)));
            _output.WriteLine($"\r\nIR: {check.PrintIR()}");

            var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);

            if (result is ErrorValue ev)
            {
                Assert.Fail($"Error: {string.Join(", ", ev.Errors.Select(er => er.Message))}");
            }

            BlobValue bv = Assert.IsType<BlobValue>(result);
            Assert.Equal("TEST FILE", await bv.GetAsStringAsync(Encoding.UTF8, CancellationToken.None));
        }

        [Theory]
        [InlineData("Office365Outlook.FindMeetingTimesV2(")]
        public void IntellisenseHelpStringsOptionalParms(string expr)
        {
            var apiDocOutlook = Helpers.ReadSwagger(@"Swagger\Office_365_Outlook.json", _output);

            var config = new PowerFxConfig();
            config.AddActionConnector("Office365Outlook", apiDocOutlook, new ConsoleLogger(_output));

            var engine = new Engine(config);
            var check = engine.Check(expr, RecordType.Empty(), new ParserOptions() { AllowsSideEffects = true });

            var result = engine.Suggest(check, expr.Length);

            var overload = result.FunctionOverloads.Single();
            Assert.Equal(Intellisense.SuggestionKind.Function, overload.Kind);
            Assert.Equal("FindMeetingTimesV2({ RequiredAttendees:String,OptionalAttendees:String,ResourceAttendees:String,MeetingDuration:Decimal,Start:DateTime,End:DateTime,MaxCandidates:Decimal,MinimumAttendeePercentage:String,IsOrganizerOptional:Boolean,ActivityDomain:String })", overload.DisplayText.Text);
        }

        // Very documentation strings from the Swagger show up in the intellisense.
        [Theory]
        [InlineData("MSNWeather.CurrentWeather(", false, false)]
        [InlineData("Behavior(); MSNWeather.CurrentWeather(", true, false)]
        [InlineData("Behavior(); MSNWeather.CurrentWeather(", false, true)]
        public void IntellisenseHelpStrings(string expr, bool withAllowSideEffects, bool expectedBehaviorError)
        {
            var apiDoc = Helpers.ReadSwagger(@"Swagger\MSNWeather.json", _output);

            var config = new PowerFxConfig();
            config.AddActionConnector("MSNWeather", apiDoc, new ConsoleLogger(_output));
            config.AddBehaviorFunction();

            var engine = new Engine(config);
            var check = engine.Check(expr, RecordType.Empty(), withAllowSideEffects ? new ParserOptions() { AllowsSideEffects = true } : null);

            if (expectedBehaviorError)
            {
                Assert.Contains(check.Errors, d => d.Message == TestExtensions.GetErrBehaviorPropertyExpectedMessage());
            }
            else
            {
                Assert.DoesNotContain(check.Errors, d => d.Message == TestExtensions.GetErrBehaviorPropertyExpectedMessage());
            }

            var result = engine.Suggest(check, expr.Length);

            var overload = result.FunctionOverloads.Single();
            Assert.Equal(Intellisense.SuggestionKind.Function, overload.Kind);
            Assert.Equal("CurrentWeather(Location, units)", overload.DisplayText.Text);
            Assert.Equal("Get the current weather for a location.", overload.Definition);
            Assert.Equal("The location search query. Valid inputs are City, Region, State, Country, Landmark, Postal Code, latitude and longitude", overload.FunctionParameterDescription);

            var sig = result.SignatureHelp.Signatures.Single();
            Assert.Equal("Get the current weather for a location.", sig.Documentation);
            Assert.Equal("CurrentWeather(Location, units)", sig.Label);

            Assert.Equal(2, sig.Parameters.Length);
            var p0 = sig.Parameters[0];
            var p1 = sig.Parameters[1];

            Assert.Equal("Location", p0.Label);
            Assert.Equal("The location search query. Valid inputs are City, Region, State, Country, Landmark, Postal Code, latitude and longitude", p0.Documentation);

            Assert.Equal("units", p1.Label);
            Assert.Equal("The measurement system used for all the meausre values in the request and response. Valid options are 'Imperial' and 'Metric'.", p1.Documentation);
        }

        [Fact]
        public void PowerPlatformConnectorClientCtor()
        {
            using var httpClient = new HttpClient();

            var ex = Assert.Throws<PowerFxConnectorException>(() => new PowerPlatformConnectorClient(
                "http://firstrelease-001.azure-apim.net:117",  // endpoint not allowed with http scheme
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient));

            Assert.Equal("Cannot accept unsecure endpoint", ex.Message);

            using var ppcl1 = new PowerPlatformConnectorClient(
                "https://firstrelease-001.azure-apim.net:117", // endpoint is valid with https scheme
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient);

            Assert.NotNull(ppcl1);
            Assert.Equal("firstrelease-001.azure-apim.net:117", ppcl1.Endpoint);

            using var ppcl2 = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net:117",         // endpoint is valid with no scheme (https assumed)
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient);

            Assert.NotNull(ppcl2);
            Assert.Equal("firstrelease-001.azure-apim.net:117", ppcl2.Endpoint);

            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json", _output);
            var apiDoc = testConnector._apiDocument;

            using var ppcl3 = new PowerPlatformConnectorClient(
                apiDoc,                                        // Swagger file with "host" param
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient);

            Assert.NotNull(ppcl3);
            Assert.Equal("localhost:23340", ppcl3.Endpoint);

            using var testConnector2 = new LoggingTestServer(@"Swagger\TestOpenAPI.json", _output);
            var apiDoc2 = testConnector2._apiDocument;

            var ex2 = Assert.Throws<PowerFxConnectorException>(() => new PowerPlatformConnectorClient(
                apiDoc2,                                        // Swagger file without "host" param
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient));

            Assert.Equal("Swagger document doesn't contain an endpoint", ex2.Message);
        }

        [Fact]
        public async Task Office365Users_UserProfile_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "02199f4f-8306-4996-b1c3-1b6094c2b7f8"
            };

            config.AddActionConnector("Office365Users", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);

            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Users", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_MyProfile_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            config.AddActionConnector("Office365Users", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Users", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.MyProfileV2().mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_DirectReports_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            config.AddActionConnector("Office365Users", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Users", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office365_DirectsV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.DirectReportsV2(""jmstall@microsoft.com"", {'$top': 4 }).value).city", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.IsType<StringValue>(result);
            Assert.Equal("Paris", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_SearchUsers_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            config.AddActionConnector("Office365Users", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Users", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office365_SearchV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.SearchUserV2({searchTerm:""Doe"", top: 3}).value).DisplayName", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.IsType<StringValue>(result);
            Assert.Equal("John Doe", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_UseDates()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "c112a9268f2a419bb0ced71f5e48ece9",              // connectionId
                    () => "ey...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc, new ConsoleLogger(_output));
            Assert.True(functions.First(f => f.Name == "GetEmailsV2").IsDeprecated);
            Assert.False(functions.First(f => f.Name == "SendEmailV2").IsDeprecated);

            // deprecated functions are supported and should not expose NotSupportedReason
            Assert.Equal(string.Empty, functions.First(f => f.Name == "GetEmailsV2").NotSupportedReason);
            Assert.Equal(string.Empty, functions.First(f => f.Name == "SendEmailV2").NotSupportedReason);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetEmails.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetEmails({ top : 5 })).DateTimeReceived", CancellationToken.None, runtimeConfig: runtimeConfig);

            Assert.IsType<DateTimeValue>(result);

            // Convert to UTC so that we don't depend on local machine settings
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc((result as DateTimeValue).GetConvertedValue(null)); // null = no conversion

            Assert.Equal(new DateTime(2023, 6, 6, 5, 4, 59, DateTimeKind.Utc), utcTime);
        }

        [Fact]
        public async Task Office365Outlook_GetEmailsV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1QiL...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetEmailsV2.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetEmailsV2().value).Id", CancellationToken.None, runtimeConfig: runtimeConfig);
            Assert.Equal(@"AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwBBHsoKDHXPEaP6AIBfULPVAAAABp8rAABDuyuwiYTvQLeL0nv55lGwAAVEFDU0AAA=", (result as StringValue).Value);

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1QiL...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: GET
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/v2/Mail?folderPath=Inbox&importance=Any&fetchOnlyWithAttachment=False&fetchOnlyUnread=True&fetchOnlyFlagged=False&includeAttachments=False&top=10
 x-ms-user-agent: PowerFx/{version}
";

            AssertEqual(expected, actual);
        }

        internal class TestClockService : IClockService
        {
            public DateTime UtcNow { get; set; } = new DateTime(2023, 6, 2, 3, 15, 7, DateTimeKind.Utc);
        }

        [Theory]
        [InlineData(1, @"Office365Outlook.V4CalendarPostItem(""Calendar"", ""Subject"", Today(), Today(), ""(UTC+01:00) Brussels, Copenhagen, Madrid, Paris"")")]
        [InlineData(2, @"Office365Outlook.V4CalendarPostItem(""Calendar"", ""Subject"", DateTime(2023, 6, 2, 11, 00, 00), DateTime(2023, 6, 2, 11, 30, 00), ""(UTC+01:00) Brussels, Copenhagen, Madrid, Paris"")")]
        public async Task Office365Outlook_V4CalendarPostItem(int id, string expr)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);

            RuntimeConfig runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetClock(new TestClockService());
            runtimeConfig.SetTimeZone(TimeZoneInfo.Utc);
            runtimeConfig.AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook V4CalendarPostItem.json");
            FormulaValue result = await engine.EvalAsync(expr, CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.Equal(@"![body:s, categories:*[Value:s], createdDateTime:d, end:d, endWithTimeZone:d, iCalUId:s, id:s, importance:s, isAllDay:b, isHtml:b, isReminderOn:b, lastModifiedDateTime:d, location:s, numberOfOccurences:w, optionalAttendees:s, organizer:s, recurrence:s, recurrenceEnd:D, reminderMinutesBeforeStart:w, requiredAttendees:s, resourceAttendees:s, responseRequested:b, responseTime:d, responseType:s, sensitivity:s, seriesMasterId:s, showAs:s, start:d, startWithTimeZone:d, subject:s, timeZone:s, webLink:s]", result.Type._type.ToString());

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: POST
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/datasets/calendars/v4/tables/Calendar/items
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""subject"":""Subject"",""start"":""2023-06-02T{(id == 1 ? "00" : "11")}:00:00.000"",""end"":""2023-06-02T{(id == 1 ? "00:00" : "11:30")}:00.000"",""timeZone"":""(UTC\u002B01:00) Brussels, Copenhagen, Madrid, Paris""}}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_ExportEmailV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1QiLCJhb...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook ExportEmailV2.txt");
            FormulaValue result = await engine.EvalAsync(@"Office365Outlook.ExportEmailV2(""AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwCDi7i2pr6zRbiq9q8hHM-iAAAFMQAZAABDuyuwiYTvQLeL0nv55lGwAAVHeZkhAAA="")", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.StartsWith("Received: from DBBPR83MB0507.EURPRD83", await (result as BlobValue).Content.GetAsStringAsync(Encoding.UTF8, CancellationToken.None));

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhb...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: GET
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/codeless/beta/me/messages/AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwCDi7i2pr6zRbiq9q8hHM-iAAAFMQAZAABDuyuwiYTvQLeL0nv55lGwAAVHeZkhAAA%3d/$value
 x-ms-user-agent: PowerFx/{version}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_FindMeetingTimesV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1QiL...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook FindMeetingTimesV2.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.FindMeetingTimesV2().meetingTimeSuggestions).meetingTimeSlot.start", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.Equal(@"{dateTime:""2023-09-26T12:00:00.0000000"",timeZone:""UTC""}", result.ToExpression());

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1QiL...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: POST
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/codeless/beta/me/findMeetingTimes
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""ActivityDomain"":""Work""}}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_FlagV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1Q...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook FlagV2.json");
            FormulaValue result = await engine.EvalAsync(@"Office365Outlook.FlagV2(""AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwBBHsoKDHXPEaP6AIBfULPVAAAABp8rAABDuyuwiYTvQLeL0nv55lGwAAVHeWXcAAA="", {flag: {flagStatus: ""flagged""}})", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.True(result is not ErrorValue);

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1Q...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: PATCH
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/codeless/v1.0/me/messages/AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwBBHsoKDHXPEaP6AIBfULPVAAAABp8rAABDuyuwiYTvQLeL0nv55lGwAAVHeWXcAAA%3d/flag
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""flag"":{{""flagStatus"":""flagged""}}}}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_GetMailTipsV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJKV1QiL...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetMailTipsV2.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetMailTipsV2(""maxMessageSize"", [""jj@microsoft.com""]).value).maxMessageSize", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.Equal(37748736m, (result as DecimalValue).Value);

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1QiL...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: POST
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/codeless/v1.0/me/getMailTips
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""MailTipsOptions"":""maxMessageSize"",""EmailAddresses"":[""jj@microsoft.com""]}}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_GetRoomListsV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "785da26033fe4f3f8604273d25f209d5",              // connectionId
                    () => "eyJ0eXAiOiJK...",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("Office365Outlook", apiDoc);
            ConnectorFunction getRoomListsV2 = functions.First(f => f.Name == "GetRoomListsV2");

            Assert.Equal("![value:*[address:s, name:s]]", getRoomListsV2.ReturnType._type.ToString());

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetRoomListsV2.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetRoomListsV2().value).address", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.Equal("li-rl-1000HENRY@microsoft.com", (result as StringValue).Value);

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJK...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: GET
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/codeless/beta/me/findRoomLists
 x-ms-user-agent: PowerFx/{version}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_Load()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            // There are 20 internal functions
            IReadOnlyList<ConnectorFunction> fi = config.AddActionConnector("Office365Outlook", apiDoc, new ConsoleLogger(_output));
            Assert.Equal(77, fi.Count());

            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("Office365Outlook", apiDoc, new ConsoleLogger(_output));
            Assert.Equal(77, functions.Count());

            ConnectorFunction cf = functions.First(cf => cf.Name == "ContactPatchItemV2");

            // Validate required parameter name
            Assert.Equal("id", cf.RequiredParameters[1].Name);

            // Validate optional parameter rectified name
            Assert.Equal("id_1", cf.OptionalParameters[0].Name);
        }

        [Fact]
        public async Task Office365Outlook_GetRooms()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",          // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",     // environment
                    "c112a9268f2a419bb0ced71f5e48ece9",         // connectionId
                    () => "eyJ0eXAiOiJK....",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            // GetRoomsV2 is not internal
            config.AddActionConnector(new ConnectorSettings("Office365Outlook") { AllowUnsupportedFunctions = true, IncludeInternalFunctions = false }, apiDoc, new ConsoleLogger(_output));

            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetRooms.json");
            var result = await engine.EvalAsync(@"Office365Outlook.GetRoomsV2()", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);

            var record = result as RecordValue;
            Assert.NotNull(record);
            var table = record.GetField("value") as TableValue;
            string expected = @"[{""address"":""room_seattle@microsoft.com"",""name"":""Seattle Room""},{""address"":""room_paris@microsoft.com"",""name"":""Paris Room""}]";
            Assert.Equal(expected, JsonSerializer.Serialize(table.ToObject()));

            string actual = testConnector._log.ToString();
            string version = PowerPlatformConnectorClient.Version;
            string expected2 = @$"POST https://firstrelease-001.azure-apim.net/invoke
 authority: firstrelease-001.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJK....
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/839eace6-59ab-4243-97ec-a5b8fcc104e4
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/office365/c112a9268f2a419bb0ced71f5e48ece9/codeless/beta/me/findRooms
 x-ms-user-agent: PowerFx/{version}
";

            Assert.Equal(expected2, actual);
        }

        [Fact]
        public async Task BingMaps_GetRouteV3()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Bing_Maps.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net",   // endpoint
                    "b60ed9ea-c17c-e39a-8682-e33a20d51e14",          // environment
                    "9ab2342eaecc4800a5c327290abf4a1f",              // connectionId
                    () => "eyJ0eXAiOiJKV1Qi....",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector("BingMaps", apiDoc);
            ConnectorFunction getRouteV3 = functions.First(f => f.Name == "GetRouteV3");

            Assert.Equal("![distanceUnit:s, durationUnit:s, routeLegs:![actualEnd:![coordinates:![combined:s, latitude:w, longitude:w], type:s], actualStart:![coordinates:![combined:s, latitude:w, longitude:w], type:s], description:s, endLocation:![address:![countryRegion:s, formattedAddress:s], confidence:s, entityType:s, name:s], routeRegion:s, startLocation:![address:![countryRegion:s, formattedAddress:s], confidence:s, entityType:s, name:s]], trafficCongestion:s, trafficDataUsed:s, travelDistance:w, travelDuration:w, travelDurationTraffic:w]", getRouteV3.ReturnType._type.ToString());

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("BingMaps", client));

            testConnector.SetResponseFromFile(@"Responses\Bing Maps GetRouteV3.json");
            FormulaValue result = await engine.EvalAsync(@"BingMaps.GetRouteV3(""Driving"", ""47,396846, -0,499967"", ""47,395142, -0,480142"").travelDuration", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig);
            Assert.Equal(260m, (result as DecimalValue).Value);

            var actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            var expected = @$"POST https://b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net/invoke
 authority: b60ed9ea-c17c-e39a-8682-e33a20d51e14.15.common.tip1eu.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1Qi....
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b60ed9ea-c17c-e39a-8682-e33a20d51e14
 x-ms-client-session-id: ce55fe97-6e74-4f56-b8cf-529e275b253f
 x-ms-request-method: GET
 x-ms-request-url: /apim/bingmaps/9ab2342eaecc4800a5c327290abf4a1f/V3/REST/V1/Routes/Driving?wp.0=47%2c396846%2c+-0%2c499967&wp.1=47%2c395142%2c+-0%2c480142&avoid_highways=False&avoid_tolls=False&avoid_ferry=False&avoid_minimizeHighways=False&avoid_minimizeTolls=False&avoid_borderCrossing=False
 x-ms-user-agent: PowerFx/{version}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task SQL_GetStoredProcs()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "ey...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddActionConnector(new ConnectorSettings("SQL") { IncludeInternalFunctions = true }, apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\SQL Server GetProceduresV2.json");
            var result = await engine.EvalAsync(@"SQL.GetProceduresV2(""pfxdev-sql.database.windows.net"", ""connectortest"")", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);

            var record = result as RecordValue;
            Assert.NotNull(record);
            var table = record.GetField("value") as TableValue;
            string expected = @"[{""DisplayName"":""[dbo].[sp_1]"",""Name"":""[dbo].[sp_1]""},{""DisplayName"":""[dbo].[sp_2]"",""Name"":""[dbo].[sp_2]""}]";
            Assert.Equal(expected, JsonSerializer.Serialize(table.ToObject()));

            string actual = testConnector._log.ToString();
            string version = PowerPlatformConnectorClient.Version;
            string expected2 = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer ey...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures
 x-ms-user-agent: PowerFx/{version}
";

            Assert.Equal(expected2, actual);
        }

        [Fact]
        public async Task SQL_ExecuteStoredProc()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "eyJ0eX...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddActionConnector("SQL", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\SQL Server ExecuteStoredProcedureV2.json");
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);

            Assert.Equal(FormulaType.UntypedObject, result.Type);
            Assert.True((result as UntypedObjectValue).Impl.TryGetPropertyNames(out IEnumerable<string> propertyNames));
            Assert.Equal(3, propertyNames.Count());

            string actual = testConnector._log.ToString();
            string version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eX...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: POST
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures/sp_1
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""p1"":50}}
";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task SQL_ExecuteStoredProc_WithUserAgent()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "eyJ0eX...",
                    "MyProduct/v1.2",                           // UserAgent to include
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddActionConnector("SQL", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\SQL Server ExecuteStoredProcedureV2.json");
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);

            Assert.Equal(FormulaType.UntypedObject, result.Type);
            Assert.True((result as UntypedObjectValue).Impl.TryGetPropertyNames(out IEnumerable<string> propertyNames));
            Assert.Equal(3, propertyNames.Count());

            string actual = testConnector._log.ToString();
            string version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eX...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: POST
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures/sp_1
 x-ms-user-agent: MyProduct/v1.2 PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""p1"":50}}
";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task SQL_ExecuteStoredProc_WithEmptyServerResponse()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient("tip1-shared-002.azure-apim.net", "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46", "5f57ec83acef477b8ccc769e52fa22cc", () => "eyJ0eX...", "MyProduct/v1.2", httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddActionConnector("SQL", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\EmptyResponse.json");
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);
            Assert.True(result is BlankValue);
        }

        [Fact]
        public async Task SQL_ExecuteStoredProc_WithInvalidResponse()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient("tip1-shared-002.azure-apim.net", "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46", "5f57ec83acef477b8ccc769e52fa22cc", () => "eyJ0eX...", "MyProduct/v1.2", httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddActionConnector("SQL", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Invalid.txt");
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);

            ErrorValue ev = Assert.IsType<ErrorValue>(result);
            string message = ev.Errors[0].Message;

            Assert.Equal(@$"SQL.ExecuteProcedureV2 failed: JsonReaderException '+' is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 0.", message);
        }

        [Fact]
        public async Task SharePointOnlineTest()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXA...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "2f0cc19d-893e-e765-b15d-2906e3231c09" /* env */, "6fb0a1a8e2f5487eafbe306821d8377e" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions("SP", apiDoc, new ConsoleLogger(_output)).OrderBy(f => f.Name).ToList();
            Assert.Equal(51, functions.Count);

            functions = OpenApiParser.GetFunctions(new ConnectorSettings("SP") { IncludeInternalFunctions = true }, apiDoc, new ConsoleLogger(_output)).OrderBy(f => f.Name).ToList();

            // The difference is due to internal functions
            Assert.Equal(101, functions.Count);
            Assert.Equal(101 - 51, functions.Count(f => f.IsInternal));

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(
                new ConnectorSettings("SP")
                {
                    // This shouldn't be used with expressions but this is OK here as this is a tabular connector and has no impact for this connector/these functions
                    Compatibility = ConnectorCompatibility.SwaggerCompatibility,
                    IncludeInternalFunctions = true,
                    ReturnUnknownRecordFieldsAsUntypedObjects = true
                },
                apiDoc,
                new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SP", ppClient, console: _output));

            // -> https://auroraprojopsintegration01.sharepoint.com/sites/Site17
            testConnector.SetResponseFromFile(@"Responses\SPO_Response1.json");
            FormulaValue fv1 = await engine.EvalAsync(@$"SP.GetDataSets()", CancellationToken.None, runtimeConfig: rc);
            string dataset = ((StringValue)((TableValue)((RecordValue)fv1).GetField("value")).Rows.First().Value.GetField("Name")).Value;

            testConnector.SetResponseFromFile(@"Responses\SPO_Response2.json");
            FormulaValue fv2 = await engine.EvalAsync(@$"SP.GetDataSetsMetadata()", CancellationToken.None, runtimeConfig: rc);
            Assert.Equal("double", ((StringValue)((RecordValue)((RecordValue)fv2).GetField("blob")).GetField("urlEncoding")).Value);

            // -> 3756de7d-cb20-4014-bab8-6ea7e5264b97
            testConnector.SetResponseFromFile(@"Responses\SPO_Response3.json");
            FormulaValue fv3 = await engine.EvalAsync($@"SP.GetAllTables(""{dataset}"")", CancellationToken.None, runtimeConfig: rc);
            string table = ((StringValue)((TableValue)((RecordValue)fv3).GetField("value")).Rows.First().Value.GetField("Name")).Value;

            testConnector.SetResponseFromFile(@"Responses\SPO_Response4.json");
            FormulaValue fv4 = await engine.EvalAsync($@"SP.GetTableViews(""{dataset}"", ""{table}"")", CancellationToken.None, runtimeConfig: rc);
            Assert.Equal("1e54c4b5-2a59-4a2a-9633-cc611a2ff718", ((StringValue)((TableValue)fv4).Rows.Skip(1).First().Value.GetField("Name")).Value);

            testConnector.SetResponseFromFile(@"Responses\SPO_Response5.json");
            FormulaValue fv5 = await engine.EvalAsync($@"SP.GetItems(""{dataset}"", ""{table}"", {{'$top': 4}})", CancellationToken.None, runtimeConfig: rc);
            Assert.Equal("Shared Documents/Document.docx", ((UntypedObjectValue)((RecordValue)((TableValue)((RecordValue)fv5).GetField("value")).Rows.First().Value).GetField("{FullPath}")).Impl.GetString());

            string version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/2f0cc19d-893e-e765-b15d-2906e3231c09
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/sharepointonline/6fb0a1a8e2f5487eafbe306821d8377e/datasets
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/2f0cc19d-893e-e765-b15d-2906e3231c09
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/sharepointonline/6fb0a1a8e2f5487eafbe306821d8377e/$metadata.json/datasets
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/2f0cc19d-893e-e765-b15d-2906e3231c09
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/sharepointonline/6fb0a1a8e2f5487eafbe306821d8377e/datasets/https%253a%252f%252fauroraprojopsintegration01.sharepoint.com%252fsites%252fSite17/alltables
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/2f0cc19d-893e-e765-b15d-2906e3231c09
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/sharepointonline/6fb0a1a8e2f5487eafbe306821d8377e/datasets/https%253a%252f%252fauroraprojopsintegration01.sharepoint.com%252fsites%252fSite17/tables/3756de7d-cb20-4014-bab8-6ea7e5264b97/views
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/2f0cc19d-893e-e765-b15d-2906e3231c09
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/sharepointonline/6fb0a1a8e2f5487eafbe306821d8377e/datasets/https%253a%252f%252fauroraprojopsintegration01.sharepoint.com%252fsites%252fSite17/tables/3756de7d-cb20-4014-bab8-6ea7e5264b97/items?$top=4
 x-ms-user-agent: PowerFx/{version}
";

            Assert.Equal(expected, testConnector._log.ToString());
        }

        [Fact]
        public async Task EdenAITest()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\Eden AI.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;

            ConnectorSettings connectorSettings = new ConnectorSettings("edenai")
            {
                Compatibility = ConnectorCompatibility.SwaggerCompatibility,
                AllowUnsupportedFunctions = true,
                IncludeInternalFunctions = true
            };

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(connectorSettings, apiDoc).OrderBy(f => f.Name).ToList();

            Assert.False(functions.First(f => f.Name == "ReceiptParser").IsSupported);
            Assert.Equal("Body with multiple parameters is not supported when one of the parameters is of type 'blob'", functions.First(f => f.Name == "ReceiptParser").NotSupportedReason);
        }

        [Fact]
        public async Task SendEmail()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\shared_sendmail.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;

            ConnectorSettings connectorSettings = new ConnectorSettings("sendmail")
            {
                Compatibility = ConnectorCompatibility.SwaggerCompatibility
            };

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(connectorSettings, apiDoc).OrderBy(f => f.Name).ToList();

            Assert.Single(functions.Where(x => x.Name == "SendEmailV3"));
        }

        [Fact]
        public async Task ExcelOnlineTest()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\ExcelOnlineBusiness.swagger.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOiJ...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "36897fc0-0c0c-eee5-ac94-e12765496c20" /* env */, "b20e87387f9149e884bdf0b0c87a67e8" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            ConnectorSettings connectorSettings = new ConnectorSettings("exob")
            {
                // This shouldn't be used with expressions but this is OK here as this is a tabular connector
                Compatibility = ConnectorCompatibility.SwaggerCompatibility,
                AllowUnsupportedFunctions = true,
                IncludeInternalFunctions = true,
                ReturnUnknownRecordFieldsAsUntypedObjects = true
            };
            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(connectorSettings, apiDoc).OrderBy(f => f.Name).ToList();

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output, true));
            RecalcEngine engine = new RecalcEngine(config);
            BasicServiceProvider serviceProvider = new BasicServiceProvider().AddRuntimeContext(new TestConnectorRuntimeContext("exob", ppClient, null, _output, true));
            RuntimeConfig runtimeConfig = new RuntimeConfig() { ServiceProvider = serviceProvider };

            CheckResult checkResult = engine.Check("exob.", symbolTable: null);
            IIntellisenseResult suggestions = engine.Suggest(checkResult, 5, serviceProvider);
            List<string> suggestedFuncs = suggestions.Suggestions.Select(s => s.DisplayText.Text).ToList();

            string source = "me";

            // Get "OneDrive" id = "b!kHbNLXp37U2hyy89eRtZD4Re_7zFnR1MsTMqs1_ocDwJW-sB0ZfqQ5NCc9L-sxKb"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response1.json");
            FormulaValue fv1 = await engine.EvalAsync(@$"exob.GetDrives(""{source}"")", CancellationToken.None, runtimeConfig: runtimeConfig);
            string drive = ((StringValue)((TableValue)((RecordValue)fv1).GetField("value")).Rows.First((DValue<RecordValue> row) => ((StringValue)row.Value.GetField("name")).Value == "OneDrive").Value.GetField("id")).Value;

            // Get file id for "AM Site.xlxs" = "01UNLFRNUJPD7RJTFEMVBZZVLQIXHAKAOO"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response2.json");
            FormulaValue fv2 = await engine.EvalAsync(@$"exob.ListRootFolder(""{source}"", ""{drive}"")", CancellationToken.None, runtimeConfig: runtimeConfig);
            string file = ((StringValue)((TableValue)fv2).Rows.First((DValue<RecordValue> row) => row.Value.GetField("Name") is StringValue sv && sv.Value == "AM Site.xlsx").Value.GetField("Id")).Value;

            // Get "Table1" id = "{00000000-000C-0000-FFFF-FFFF00000000}"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response3.json");
            FormulaValue fv3 = await engine.EvalAsync(@$"exob.GetTables(""{source}"", ""{drive}"", ""{file}"")", CancellationToken.None, runtimeConfig: runtimeConfig);
            string table = ((StringValue)((TableValue)((RecordValue)fv3).GetField("value")).Rows.First((DValue<RecordValue> row) => ((StringValue)row.Value.GetField("name")).Value == "Table1").Value.GetField("id")).Value;

            // Get PowerApps id for 2nd row = "f830UPeAXoI"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response4.json");
            FormulaValue fv4 = await engine.EvalAsync(@$"exob.GetItems(""{source}"", ""{drive}"", ""{file}"", ""{table}"")", CancellationToken.None, runtimeConfig: runtimeConfig);
            string columnId = ((UntypedObjectValue)((RecordValue)((TableValue)((RecordValue)fv4).GetField("value")).Rows.Skip(1).First().Value).GetField("__PowerAppsId__")).Impl.GetString();

            // Get Item by columnId
            testConnector.SetResponseFromFile(@"Responses\EXO_Response5.json");
            FormulaValue fv5 = await engine.EvalAsync(@$"exob.GetItem(""{source}"", ""{drive}"", ""{file}"", ""{table}"", ""__PowerAppsId__"", ""{columnId}"")", CancellationToken.None, runtimeConfig: runtimeConfig);
            RecordValue rv5 = (RecordValue)fv5;

            Assert.Equal(FormulaType.UntypedObject, rv5.GetField("Site").Type);
            Assert.Equal("Atlanta", ((UntypedObjectValue)rv5.GetField("Site")).Impl.GetString());

            // Network trace cannot be tested here as SwaggerCompatibility is enabled.
        }

        [Fact]
        public async Task DataverseTest_WithComplexMapping()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Dataverse.json", _output);
            using var httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient("https://tip1002-002.azure-apihub.net", "b29c41cf-173b-e469-830b-4f00163d296b" /* environment Id */, "82728ddb6bfa461ea3e50e17da8ab164" /* connectionId */, () => "eyJ0eXAiOiJKV1QiLCJ...", httpClient) { SessionId = "a41bd03b-6c3c-4509-a844-e8c51b61f878" };

            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext("DV", client, console: _output);
            ConnectorFunction[] functions = OpenApiParser.GetFunctions(new ConnectorSettings("DV") { Compatibility = ConnectorCompatibility.SwaggerCompatibility }, testConnector._apiDocument).ToArray();
            ConnectorFunction performBoundActionWithOrganization = functions.First(f => f.Name == "PerformBoundActionWithOrganization");

            testConnector.SetResponseFromFile(@"Responses\Dataverse_Response_3.json");
            ConnectorParameters parameters = await performBoundActionWithOrganization.GetParameterSuggestionsAsync(
                new NamedValue[]
                {
                    new NamedValue("organization", FormulaValue.New("https://aurorabapenv9984a.crm10.dynamics.com/")),
                    new NamedValue("entityName", FormulaValue.New("bots")),
                },
                performBoundActionWithOrganization.RequiredParameters[2], // actionName
                runtimeContext,
                CancellationToken.None);

            // Received 8 suggestions
            Assert.Equal(8, parameters.ParametersWithSuggestions[2].Suggestions.Count());

            string actual = testConnector._log.ToString();
            var version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1002-002.azure-apihub.net/invoke
 authority: tip1002-002.azure-apihub.net
 Authorization: Bearer eyJ0eXAiOiJKV1QiLCJ...
 organization: https://aurorabapenv9984a.crm10.dynamics.com/
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/b29c41cf-173b-e469-830b-4f00163d296b
 x-ms-client-session-id: a41bd03b-6c3c-4509-a844-e8c51b61f878
 x-ms-request-method: POST
 x-ms-request-url: /apim/commondataserviceforapps/82728ddb6bfa461ea3e50e17da8ab164/v1.0/$metadata.json/GetActionListEnum/GetBoundActionsWithOrganization
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""entityName"":""bots""}}
";

            Assert.Equal(expected, actual);
        }

        // ConnectorCompatibility element will determine if an internal parameters will be suggested.
        [Theory]
        [InlineData(ConnectorCompatibility.Default, "Office365Users.SearchUserV2(", "SearchUserV2({ searchTerm:String,top:Decimal,isSearchTermRequired:Boolean,skipToken:String })")]
        [InlineData(ConnectorCompatibility.SwaggerCompatibility, "Office365Users.SearchUserV2(", "SearchUserV2({ searchTerm:String,top:Decimal,isSearchTermRequired:Boolean })")]
        public async Task ConnectorCompatibilityIntellisenseTest(ConnectorCompatibility compact, string expression, string expected)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);

            using var client = new PowerPlatformConnectorClient(
                    "firstrelease-001.azure-apim.net",               // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4",          // environment
                    "72c42ee1b3c7403c8e73aa9c02a7fbcc",              // connectionId
                    () => "Some JWT token",
                    httpClient)
            {
                SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
            };

            var split = expression.Split('.');

            ConnectorSettings connectorSettings = new ConnectorSettings(split[0])
            {
                Compatibility = compact
            };

            config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));

            var engine = new RecalcEngine(config);
            var suggestions = engine.Suggest(expression, null, expression.Length);
            var overload = suggestions.FunctionOverloads.First();

            Assert.Equal(expected, overload.DisplayText.Text);
        }

        [Fact]
        public void Connector_UnsupportedDate()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\AzureAppService.json", _output);
            List<ConnectorFunction> funcs = OpenApiParser.GetFunctions(new ConnectorSettings("AAS") { AllowUnsupportedFunctions = true, IncludeInternalFunctions = true }, testConnector._apiDocument, new ConsoleLogger(_output)).ToList();
            ConnectorFunction resourceGroupsList = funcs.First(f => f.Name == "ResourceGroupsList");

            Assert.True(resourceGroupsList.IsSupported);
            Assert.Single(resourceGroupsList.HiddenRequiredParameters);
            Assert.Equal("x-ms-api-version", resourceGroupsList.HiddenRequiredParameters[0].Name);
            StringValue sv = Assert.IsType<StringValue>(resourceGroupsList.HiddenRequiredParameters[0].DefaultValue);
            Assert.Equal("2020-01-01", sv.Value);
        }

        [Fact]
        public async Task DVDynamicReturnType()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\Dataverse.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;

            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiO..";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1002-002.azure-apihub.net", "ba347af5-05f5-e331-a109-ad48533ebffc" /* env */, "393a9f94ee8841e7887da9d706387c33" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            ConnectorSettings connectorSettings = new ConnectorSettings("cds") { Compatibility = ConnectorCompatibility.SwaggerCompatibility };
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext("cds", ppClient, console: _output);
            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(connectorSettings, apiDoc).OrderBy(f => f.Name).ToList();
            ConnectorFunction listRecordsWithOrganizations = functions.First(functions => functions.Name == "ListRecordsWithOrganization");

            NamedValue[] parameters = new NamedValue[]
            {
                new NamedValue("organization", FormulaValue.New("https://aurorabapenv969d7.crm10.dynamics.com")),
                new NamedValue("entityName", FormulaValue.New("accounts"))
            };

            testConnector.SetResponseFromFiles(Enumerable.Range(0, 23).Select(i => $@"Responses\Response_DVReturnType_{i:00}.json").ToArray());
            ConnectorType returnType = await listRecordsWithOrganizations.GetConnectorReturnTypeAsync(parameters, runtimeContext, 23, CancellationToken.None);
            string ft = returnType.FormulaType.ToStringWithDisplayNames();

            string expected =
                "!['@odata.nextLink'`'Next link':s, value:*[Array:!['@odata.id'`'OData Id':s, _createdby_value`'Created By (Value)':s, '_createdby_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Created By (Type)':s, " +
                "_createdbyexternalparty_value`'Created By (External Party) (Value)':s, '_createdbyexternalparty_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Created By (External Party) (Type)':s, _createdonbehalfby_value`'Created " +
                "By (Delegate) (Value)':s, '_createdonbehalfby_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Created By (Delegate) (Type)':s, _defaultpricelevelid_value`'Price List (Value)':s, '_defaultpricelevelid_value@Microsoft.Dynamics.CRM.lookuplogic" +
                "alname'`'Price List (Type)':s, _masterid_value`'Master ID (Value)':s, '_masterid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Master ID (Type)':s, _modifiedby_value`'Modified By (Value)':s, '_modifiedby_value@Microsoft.Dynamics.CRM.looku" +
                "plogicalname'`'Modified By (Type)':s, _modifiedbyexternalparty_value`'Modified By (External Party) (Value)':s, '_modifiedbyexternalparty_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Modified By (External " +
                "Party) (Type)':s, _modifiedonbehalfby_value`'Modified By (Delegate) (Value)':s, '_modifiedonbehalfby_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Modified By (Delegate) (Type)':s, _msa_managingpartnerid_value`'Managing " +
                "Partner (Value)':s, '_msa_managingpartnerid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Managing Partner (Type)':s, _msdyn_accountkpiid_value`'KPI (Value)':s, '_msdyn_accountkpiid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'KPI " +
                "(Type)':s, _msdyn_salesaccelerationinsightid_value`'Sales Acceleration Insights ID (Value)':s, '_msdyn_salesaccelerationinsightid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Sales Acceleration Insights " +
                "ID (Type)':s, _originatingleadid_value`'Originating Lead (Value)':s, '_originatingleadid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Originating Lead (Type)':s, _ownerid_value`'Owner (Value)':s, '_ownerid_value@Microsoft.Dynamics.CRM.lo" +
                "okuplogicalname'`'Owner (Type)':s, _owningbusinessunit_value`'Owning Business Unit (Value)':s, '_owningbusinessunit_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Owning Business Unit (Type)':s, _owningteam_value`'Owning " +
                "Team (Value)':s, '_owningteam_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Owning Team (Type)':s, _owninguser_value`'Owning User (Value)':s, '_owninguser_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Owning " +
                "User (Type)':s, _parentaccountid_value`'Parent Account (Value)':s, '_parentaccountid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Parent Account (Type)':s, _preferredequipmentid_value`'Preferred Facility/Equipment " +
                "(Value)':s, '_preferredequipmentid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Preferred Facility/Equipment (Type)':s, _preferredserviceid_value`'Preferred Service (Value)':s, '_preferredserviceid_value@Microsoft.Dynamics.CRM.lookuplogi" +
                "calname'`'Preferred Service (Type)':s, _preferredsystemuserid_value`'Preferred User (Value)':s, '_preferredsystemuserid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Preferred User (Type)':s, _primarycontactid_value`'Primary " +
                "Contact (Value)':s, '_primarycontactid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Primary Contact (Type)':s, _slaid_value`'SLA (Value)':s, '_slaid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'SLA " +
                "(Type)':s, _slainvokedid_value`'Last SLA applied (Value)':s, '_slainvokedid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Last SLA applied (Type)':s, _territoryid_value`'Territory (Value)':s, '_territoryid_value@Microsoft.Dynamics.CRM.loo" +
                "kuplogicalname'`'Territory (Type)':s, _transactioncurrencyid_value`'Currency (Value)':s, '_transactioncurrencyid_value@Microsoft.Dynamics.CRM.lookuplogicalname'`'Currency (Type)':s, accountcategorycode`Category:w, " +
                "accountclassificationcode`Classification:w, accountid`Account:s, accountnumber`'Account Number':s, accountratingcode`'Account Rating':w, address1_addressid`'Address 1: ID':s, address1_addresstypecode`'Address " +
                "1: Address Type':w, address1_city`'Address 1: City':s, address1_composite`'Address 1':s, address1_country`'Address 1: Country/Region':s, address1_county`'Address 1: County':s, address1_fax`'Address 1: " +
                "Fax':s, address1_freighttermscode`'Address 1: Freight Terms':w, address1_latitude`'Address 1: Latitude':w, address1_line1`'Address 1: Street 1':s, address1_line2`'Address 1: Street 2':s, address1_line3`'Address " +
                "1: Street 3':s, address1_longitude`'Address 1: Longitude':w, address1_name`'Address 1: Name':s, address1_postalcode`'Address 1: ZIP/Postal Code':s, address1_postofficebox`'Address 1: Post Office Box':s, " +
                "address1_primarycontactname`'Address 1: Primary Contact Name':s, address1_shippingmethodcode`'Address 1: Shipping Method':w, address1_stateorprovince`'Address 1: State/Province':s, address1_telephone1`'Address " +
                "Phone':s, address1_telephone2`'Address 1: Telephone 2':s, address1_telephone3`'Address 1: Telephone 3':s, address1_upszone`'Address 1: UPS Zone':s, address1_utcoffset`'Address 1: UTC Offset':w, address2_addressid`'Address " +
                "2: ID':s, address2_addresstypecode`'Address 2: Address Type':w, address2_city`'Address 2: City':s, address2_composite`'Address 2':s, address2_country`'Address 2: Country/Region':s, address2_county`'Address " +
                "2: County':s, address2_fax`'Address 2: Fax':s, address2_freighttermscode`'Address 2: Freight Terms':w, address2_latitude`'Address 2: Latitude':w, address2_line1`'Address 2: Street 1':s, address2_line2`'Address " +
                "2: Street 2':s, address2_line3`'Address 2: Street 3':s, address2_longitude`'Address 2: Longitude':w, address2_name`'Address 2: Name':s, address2_postalcode`'Address 2: ZIP/Postal Code':s, address2_postofficebox`'Address " +
                "2: Post Office Box':s, address2_primarycontactname`'Address 2: Primary Contact Name':s, address2_shippingmethodcode`'Address 2: Shipping Method':w, address2_stateorprovince`'Address 2: State/Province':s, " +
                "address2_telephone1`'Address 2: Telephone 1':s, address2_telephone2`'Address 2: Telephone 2':s, address2_telephone3`'Address 2: Telephone 3':s, address2_upszone`'Address 2: UPS Zone':s, address2_utcoffset`'Address " +
                "2: UTC Offset':w, adx_createdbyipaddress`'Created By (IP Address)':s, adx_createdbyusername`'Created By (User Name)':s, adx_modifiedbyipaddress`'Modified By (IP Address)':s, adx_modifiedbyusername`'Modified " +
                "By (User Name)':s, aging30`'Aging 30':w, aging30_base`'Aging 30 (Base)':w, aging60`'Aging 60':w, aging60_base`'Aging 60 (Base)':w, aging90`'Aging 90':w, aging90_base`'Aging 90 (Base)':w, businesstypecode`'Business " +
                "Type':w, createdon`'Created On':d, creditlimit`'Credit Limit':w, creditlimit_base`'Credit Limit (Base)':w, creditonhold`'Credit Hold':b, customersizecode`'Customer Size':w, customertypecode`'Relationship " +
                "Type':w, description`Description:s, donotbulkemail`'Do not allow Bulk Emails':b, donotbulkpostalmail`'Do not allow Bulk Mails':b, donotemail`'Do not allow Emails':b, donotfax`'Do not allow Faxes':b, donotphone`'Do " +
                "not allow Phone Calls':b, donotpostalmail`'Do not allow Mails':b, donotsendmm`'Send Marketing Materials':b, emailaddress1`Email:s, emailaddress2`'Email Address 2':s, emailaddress3`'Email Address 3':s, " +
                "entityimage`'Default Image':s, entityimageid`'Entity Image Id':s, exchangerate`'Exchange Rate':w, fax`Fax:s, followemail`'Follow Email Activity':b, ftpsiteurl`'FTP Site':s, importsequencenumber`'Import " +
                "Sequence Number':w, industrycode`Industry:w, lastonholdtime`'Last On Hold Time':d, lastusedincampaign`'Last Date Included in Campaign':d, marketcap`'Market Capitalization':w, marketcap_base`'Market Capitalization " +
                "(Base)':w, marketingonly`'Marketing Only':b, merged`Merged:b, modifiedon`'Modified On':d, msdyn_gdproptout`'GDPR Optout':b, name`'Account Name':s, numberofemployees`'Number of Employees':w, onholdtime`'On " +
                "Hold Time (Minutes)':w, opendeals`'Open Deals':w, opendeals_date`'Open Deals (Last Updated On)':d, opendeals_state`'Open Deals (State)':w, openrevenue`'Open Revenue':w, openrevenue_base`'Open Revenue (Base)':w, " +
                "openrevenue_date`'Open Revenue (Last Updated On)':d, openrevenue_state`'Open Revenue (State)':w, overriddencreatedon`'Record Created On':d, ownershipcode`Ownership:w, participatesinworkflow`'Participates " +
                "in Workflow':b, paymenttermscode`'Payment Terms':w, preferredappointmentdaycode`'Preferred Day':w, preferredappointmenttimecode`'Preferred Time':w, preferredcontactmethodcode`'Preferred Method of Contact':w, " +
                "primarysatoriid`'Primary Satori ID':s, primarytwitterid`'Primary Twitter ID':s, processid`Process:s, revenue`'Annual Revenue':w, revenue_base`'Annual Revenue (Base)':w, sharesoutstanding`'Shares Outstanding':w, " +
                "shippingmethodcode`'Shipping Method':w, sic`'SIC Code':s, stageid`'(Deprecated) Process Stage':s, statecode`Status:w, statuscode`'Status Reason':w, stockexchange`'Stock Exchange':s, teamsfollowed`TeamsFollowed:w, " +
                "telephone1`'Main Phone':s, telephone2`'Other Phone':s, telephone3`'Telephone 3':s, territorycode`'Territory Code':w, tickersymbol`'Ticker Symbol':s, timespentbymeonemailandmeetings`'Time Spent by me':s, " +
                "timezoneruleversionnumber`'Time Zone Rule Version Number':w, traversedpath`'(Deprecated) Traversed Path':s, utcconversiontimezonecode`'UTC Conversion Time Zone Code':w, versionnumber`'Version Number':w, " +
                "websiteurl`Website:s, yominame`'Yomi Account Name':s]]]";

            Assert.Equal(expected, ft);
            Assert.Equal("address1_addresstypecode", returnType.Fields[0].Fields[0].Fields[7].Name);
            Assert.Equal("w", returnType.Fields[0].Fields[0].Fields[7].FormulaType.ToStringWithDisplayNames());
            Assert.True(returnType.Fields[0].Fields[0].Fields[7].IsEnum);
            Assert.Equal("1, 4, 3, 2", string.Join(", ", returnType.Fields[0].Fields[0].Fields[7].EnumValues.Select(ev => ev.ToObject().ToString())));
            Assert.Equal("Bill To=1, Other=4, Primary=3, Ship To=2", string.Join(", ", returnType.Fields[0].Fields[0].Fields[7].Enum.Select(kvp => $"{kvp.Key}={kvp.Value.ToObject()}")));

            // Now, only make a single network call and see the difference: none of the option set values are populated.
            testConnector.SetResponseFromFiles(Enumerable.Range(0, 1).Select(i => $@"Responses\Response_DVReturnType_{i:00}.json").ToArray());
            ConnectorType returnType2 = await listRecordsWithOrganizations.GetConnectorReturnTypeAsync(parameters, runtimeContext, CancellationToken.None);
            string ft2 = returnType2.FormulaType.ToStringWithDisplayNames();            

            Assert.Equal(expected, ft2);
            Assert.Equal("address1_addresstypecode", returnType2.Fields[0].Fields[0].Fields[7].Name);
            Assert.Equal("w", returnType2.Fields[0].Fields[0].Fields[7].FormulaType.ToStringWithDisplayNames());
            Assert.True(returnType.Fields[0].Fields[0].Fields[7].IsEnum);

            // Key differences
            Assert.Equal(string.Empty, string.Join(", ", returnType2.Fields[0].Fields[0].Fields[7].EnumValues.Select(ev => ev.ToObject().ToString())));
            Assert.Null(returnType2.Fields[0].Fields[0].Fields[7].Enum);
        }

        [Fact]
        public async Task ServiceNowOutputType_GetRecord()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\ServiceNow.json", _output);
            OpenApiDocument apiDoc = testConnector._apiDocument;

            testConnector.SetResponseFromFile($@"Responses\Response_ServiceNowReturnType.json");

            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOiJKV1QiLCJhbGc...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared.azure-apim.net", "Default-c2983f0e-34ee-4b43-8abc-c2f460fd26be" /* env */, "7dfb68206d8c46c0aa25cd75e3172d44" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            ConnectorSettings connectorSettings = new ConnectorSettings("cds") { Compatibility = ConnectorCompatibility.SwaggerCompatibility };
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext("cds", ppClient, console: _output);
            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(connectorSettings, apiDoc).OrderBy(f => f.Name).ToList();
            ConnectorFunction listRecordsWithOrganizations = functions.First(functions => functions.Name == "GetRecord");

            NamedValue[] parameters = new NamedValue[]
            {
                new NamedValue("tableType", FormulaValue.New("sn_ex_sp_action"))
            };

            ConnectorType returnType = await listRecordsWithOrganizations.GetConnectorReturnTypeAsync(parameters, runtimeContext, 23, CancellationToken.None);
            string ft = returnType.FormulaType.ToStringWithDisplayNames();

            string expected =
                "![result:![action_type`'Action type':s, active`Active:s, icon`'Action icon':s, name`'Action label':s, sys_class_name`Class:s, sys_created_by`'Created by':s, sys_created_on`Created:s, sys_domain`Domain:s, sys_id`'Sys ID':s, sys_mod_count`Updates:s, sys_name`'Display name':s, sys_overrides`Overrides:s, sys_package`Package:s, sys_policy`'Protection policy':s, sys_scope`Application:s, sys_tags`Tags:s, sys_update_name`'Update name':s, sys_updated_by`'Updated by':s, sys_updated_on`Updated:s, table`Table:s]]";

            Assert.Equal(expected, ft);
        }

        [Fact]
        public async Task SQL_ExecuteStoredProc_Scoped()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient("tip1-shared-002.azure-apim.net", "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46", "5f57ec83acef477b8ccc769e52fa22cc", () => "eyJ0eX...", httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            // Here, apart from connectionId, we define server and database as globals
            // This will modify the list of functions and their parameters as 'server' and 'database' will be removed from required parameter list
            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("5f57ec83acef477b8ccc769e52fa22cc") },
                { "server", FormulaValue.New("pfxdev-sql.database.windows.net") },
                { "database", FormulaValue.New("connectortest") }
            });

            // Action connector with global values
            IReadOnlyList<ConnectorFunction> fList = config.AddActionConnector("SQL", apiDoc, globals, new ConsoleLogger(_output, true));
            ConnectorFunction executeProcedureV2 = fList.First(f => f.Name == "ExecuteProcedureV2");

            var engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client));

            testConnector.SetResponseFromFile(@"Responses\SQL Server ExecuteStoredProcedureV2.json");

            // The list of parameters is reduced here (compare with SQL_ExecuteStoredProc test)
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc);

            Assert.Equal(FormulaType.UntypedObject, result.Type);
            Assert.True((result as UntypedObjectValue).Impl.TryGetPropertyNames(out IEnumerable<string> propertyNames));
            Assert.Equal(3, propertyNames.Count());

            string actual = testConnector._log.ToString();
            string version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eX...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: POST
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures/sp_1
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: application/json; charset=utf-8
 [body] {{""p1"":50}}
";

            Assert.Equal(expected, actual);
        }

        public class HttpLogger : HttpClient
        {
            private readonly ITestOutputHelper _console;

            public HttpLogger(ITestOutputHelper console)
                : base()
            {
                _console = console;
            }

            public HttpLogger(ITestOutputHelper console, HttpMessageHandler handler)
                : base(handler)
            {
                _console = console;
            }

            public HttpLogger(ITestOutputHelper console, HttpMessageHandler handler, bool disposeHandler)
                : base(handler, disposeHandler)
            {
                _console = console;
            }

            public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var text = response?.Content == null 
                                    ? string.Empty
#if NET7_0_OR_GREATER
                                    : await response.Content.ReadAsStringAsync(cancellationToken);
#else

                                     // We cannot pass the cancellation token in .Net 4.6.2
                                    : await response.Content.ReadAsStringAsync();
#endif
                    _console.WriteLine($"[HTTP Status {(int)response.StatusCode} {response.StatusCode} - {text}");
                }

                return response;
            }
        }
    }
}
