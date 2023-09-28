// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Connectors.Tests;
using Microsoft.PowerFx.Core.Tests;
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
            using var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json");
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
            Assert.Equal(funcNames, new string[] { "CurrentWeather", "GetMeasureUnits", "TodaysForecast", "TomorrowsForecast" });

            // Now execute it...
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("MSNWeather", client, console: _output));
            testConnector.SetResponseFromFile(@"Responses\MSNWeather_Response.json");

            var result = await engine.EvalAsync("MSNWeather.CurrentWeather(\"Redmond\", \"Imperial\").responses.weather.current.temp", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
 x-ms-request-url: /apim/msnweather/shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4/current/Redmond?units=I
 x-ms-user-agent: PowerFx/{version}
";
            AssertEqual(expected, actual);
        }

        [Theory]
        [InlineData(100)] // Continue
        [InlineData(200)] // Ok
        [InlineData(202)] // Accepted
        [InlineData(305)] // Use Proxy
        [InlineData(411)] // Length Required
        [InlineData(500)] // Server Error
        [InlineData(502)] // Bad Gateway
        public async Task Connector_GenerateErrors(int statusCode)
        {
            using var testConnector = new LoggingTestServer(@"Swagger\TestConnector12.json");
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
            var result = await engine.EvalAsync($"TestConnector12.GenerateError({{error: {statusCode}}})", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

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
                Assert.Equal(1, ev.Errors.Count);

                var err = ev.Errors[0];

                Assert.Equal(ErrorKind.Network, err.Kind);
                Assert.Equal(ErrorSeverity.Critical, err.Severity);
                Assert.Equal($"TestConnector12.GenerateError failed: The server returned an HTTP error with code {statusCode}. Response: {statusCode}", err.Message);
            }

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result2 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),FirstError.Message)", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.NotNull(result2);
            Assert.IsType<StringValue>(result2);

            var sv2 = (StringValue)result2;

            if (statusCode < 300)
            {
                Assert.Equal(statusCode.ToString(), sv2.Value);
            }
            else
            {
                Assert.Equal($"TestConnector12.GenerateError failed: The server returned an HTTP error with code {statusCode}. Response: {statusCode}", sv2.Value);
            }

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result3 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),CountRows(AllErrors))", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

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
            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();
            var token = @"eyJ0eX...";

            using var httpClient = new HttpClient(testConnector);
            using var client = useSwaggerParameter ?
                new PowerPlatformConnectorClient(
                    apiDoc,                                 // Swagger file
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46", // environment
                    "3a3239ba9a2648788a83f5172d3d4ec5",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                }
                : new PowerPlatformConnectorClient(
                    (useHttpsPrefix ? "https://" : string.Empty) +
                        "tip1-shared-002.azure-apim.net",  // endpoint
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46", // environment
                    "3a3239ba9a2648788a83f5172d3d4ec5",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                };

            var funcs = config.AddActionConnector("AzureBlobStorage", apiDoc, new ConsoleLogger(_output));
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();
            Assert.Equal(funcNames, new string[] { "AppendFile", "AppendFileV2", "CopyFile", "CopyFileOld", "CopyFileV2", "CreateBlockBlob", "CreateBlockBlobV2", "CreateFile", "CreateFileOld", "CreateFileV2", "CreateFolder", "CreateFolderV2", "CreateShareLinkByPath", "CreateShareLinkByPathV2", "DeleteFile", "DeleteFileOld", "DeleteFileV2", "ExtractFolderOld", "ExtractFolderV2", "ExtractFolderV3", "GetAccessPolicies", "GetAccessPoliciesV2", "GetDataSets", "GetDataSetsMetadata", "GetFileContent", "GetFileContentByPath", "GetFileContentByPathOld", "GetFileContentByPathV2", "GetFileContentOld", "GetFileContentV2", "GetFileMetadata", "GetFileMetadataByPath", "GetFileMetadataByPathOld", "GetFileMetadataByPathV2", "GetFileMetadataOld", "GetFileMetadataV2", "ListAllRootFolders", "ListAllRootFoldersV2", "ListAllRootFoldersV3", "ListAllRootFoldersV4", "ListFolder", "ListFolderOld", "ListFolderV2", "ListFolderV3", "ListFolderV4", "ListRootFolder", "ListRootFolderOld", "ListRootFolderV2", "ListRootFolderV3", "ListRootFolderV4", "RenameFile", "RenameFileV2", "SetBlobTierByPath", "SetBlobTierByPathV2", "TestConnection", "UpdateFile", "UpdateFileOld", "UpdateFileV2" });

            // Now execute it...
            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("AzureBlobStorage", client, console: _output)); 
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_Response.json");

            var result = await engine.EvalAsync(@"AzureBlobStorage.CreateFile(""container"", ""bora4.txt"", ""abc"").Size", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            dynamic res = result.ToObject();
            var size = (double)res;

            Assert.Equal(3.0, size);

            // PowerPlatform Connectors transform the request significantly from what was in the swagger. 
            // Some of this information comes from setting passed into connector client. 
            // Other information is from swagger. 
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var host = useSwaggerParameter ? "localhost:23340" : "tip1-shared-002.azure-apim.net";
            var expected = @$"POST https://{host}/invoke
 authority: {host}
 Authorization: Bearer eyJ0eX...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: ccccbff3-9d2c-44b2-bee6-cf24aab10b7e
 x-ms-request-method: POST
 x-ms-request-url: /apim/azureblob/3a3239ba9a2648788a83f5172d3d4ec5/datasets/default/files?folderPath=container&name=bora4.txt
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: text/plain; charset=utf-8
 [body] abc
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task AzureBlobConnector_UseOfDeprecatedFunction()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAi...";
            string expr = @"First(azbs.ListRootFolderV3(""pfxdevstgaccount1"")).DisplayName";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "36897fc0-0c0c-eee5-ac94-e12765496c20" /* env */, "d95489a91a5846f4b2c095307d86edd6" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector("azbs", apiDoc, new ConsoleLogger(_output));
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
            FormulaValue fv = await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            Assert.False(fv is ErrorValue);
            Assert.True(fv is StringValue);

            StringValue sv = (StringValue)fv;
            Assert.Equal("container", sv.Value);            
        }

        [Fact]
        public async Task AzureBlobConnector_Paging()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
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
            FormulaValue fv = await engine.EvalAsync(@"CountRows(azbs.ListFolderV4(""pfxdevstgaccount1"", ""container"").value)", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            Assert.False(fv is ErrorValue);
            Assert.True(fv is DecimalValue);
            Assert.Equal(12m, ((DecimalValue)fv).Value);

            testConnector.SetResponseFromFiles(@"Responses\AzureBlobStorage_Paging_Response1.json", @"Responses\AzureBlobStorage_Paging_Response2.json");
            fv = await engine.EvalAsync(@"CountRows(azbs2.ListFolderV4(""pfxdevstgaccount1"", ""container"").value)", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            Assert.True(fv is DecimalValue);
            Assert.Equal(7m, ((DecimalValue)fv).Value);
        }

        // Very documentation strings from the Swagger show up in the intellisense.
        [Theory]
        [InlineData("MSNWeather.CurrentWeather(", false, false)]
        [InlineData("Behavior(); MSNWeather.CurrentWeather(", true, false)]
        [InlineData("Behavior(); MSNWeather.CurrentWeather(", false, true)]
        public void IntellisenseHelpStrings(string expr, bool withAllowSideEffects, bool expectedBehaviorError)
        {
            var apiDoc = Helpers.ReadSwagger(@"Swagger\MSNWeather.json");

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

            var ex = Assert.Throws<ArgumentException>(() => new PowerPlatformConnectorClient(
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

            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
            var apiDoc = testConnector._apiDocument;

            using var ppcl3 = new PowerPlatformConnectorClient(
                apiDoc,                                        // Swagger file with "host" param
                "839eace6-59ab-4243-97ec-a5b8fcc104e4",
                "453f61fa88434d42addb987063b1d7d2",
                () => "AuthToken",
                httpClient);

            Assert.NotNull(ppcl3);
            Assert.Equal("localhost:23340", ppcl3.Endpoint);

            using var testConnector2 = new LoggingTestServer(@"Swagger\TestOpenAPI.json");
            var apiDoc2 = testConnector2._apiDocument;

            var ex2 = Assert.Throws<ArgumentException>(() => new PowerPlatformConnectorClient(
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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json");
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
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_MyProfile_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json");
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
            var result = await engine.EvalAsync(@"Office365Users.MyProfileV2().mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_DirectReports_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json");
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
            var result = await engine.EvalAsync(@"First(Office365Users.DirectReportsV2(""jmstall@microsoft.com"", {'$top': 4 }).value).city", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("Paris", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_SearchUsers_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json");
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
            var result = await engine.EvalAsync(@"First(Office365Users.SearchUserV2({searchTerm:""Doe"", top: 3}).value).DisplayName", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("John Doe", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_UseDates()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            Assert.Equal("OpenApiOperation is deprecated", functions.First(f => f.Name == "GetEmailsV2").NotSupportedReason);
            Assert.Equal(string.Empty, functions.First(f => f.Name == "SendEmailV2").NotSupportedReason);

            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetEmails.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetEmails({ top : 5 })).DateTimeReceived", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<DateTimeValue>(result);

            // Convert to UTC so that we don't depend on local machine settings
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc((result as DateTimeValue).GetConvertedValue(null)); // null = no conversion

            Assert.Equal(new DateTime(2023, 6, 6, 5, 4, 59, DateTimeKind.Utc), utcTime);
        }

        [Fact]
        public async Task Office365Outlook_GetEmailsV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetEmailsV2().value).Id", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
 x-ms-request-url: /apim/office365/785da26033fe4f3f8604273d25f209d5/v2/Mail
 x-ms-user-agent: PowerFx/{version}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_V4CalendarPostItem()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client));
            
            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook V4CalendarPostItem.json");
            FormulaValue result = await engine.EvalAsync(@"Office365Outlook.V4CalendarPostItem(""Calendar"", ""Subject"", Today(), Today(), ""(UTC+01:00) Brussels, Copenhagen, Madrid, Paris"")", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            Assert.Equal(@"![body:s, categories:*[Value:s], createdDateTime:d, end:d, endWithTimeZone:d, iCalUId:s, id:s, importance:s, isAllDay:b, isHtml:b, isReminderOn:b, lastModifiedDateTime:d, location:s, numberOfOccurences:w, optionalAttendees:s, organizer:s, recurrence:s, recurrenceEnd:d, reminderMinutesBeforeStart:w, requiredAttendees:s, resourceAttendees:s, responseRequested:b, responseTime:d, responseType:s, selectedDaysOfWeek:N, sensitivity:s, seriesMasterId:s, showAs:s, start:d, startWithTimeZone:d, subject:s, timeZone:s, webLink:s]", result.Type._type.ToString());

            var actual = testConnector._log.ToString();
            var today = DateTime.UtcNow.Date.ToString("O");
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
 [body] {{""subject"":""Subject"",""start"":""{today}"",""end"":""{today}"",""timeZone"":""(UTC\u002B01:00) Brussels, Copenhagen, Madrid, Paris""}}
";

            AssertEqual(expected, actual);
        }

        [Fact]
        public async Task Office365Outlook_ExportEmailV2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"Office365Outlook.ExportEmailV2(""AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwCDi7i2pr6zRbiq9q8hHM-iAAAFMQAZAABDuyuwiYTvQLeL0nv55lGwAAVHeZkhAAA="")", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            Assert.StartsWith("Received: from DBBPR83MB0507.EURPRD83", (result as StringValue).Value);

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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.FindMeetingTimesV2().meetingTimeSuggestions).meetingTimeSlot.start", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"Office365Outlook.FlagV2(""AAMkAGJiMDkyY2NkLTg1NGItNDg1ZC04MjMxLTc5NzQ1YTUwYmNkNgBGAAAAAACivZtRsXzPEaP8AIBfULPVBwBBHsoKDHXPEaP6AIBfULPVAAAABp8rAABDuyuwiYTvQLeL0nv55lGwAAVHeWXcAAA="", {flag: {flagStatus: ""flagged""}})", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetMailTipsV2(""maxMessageSize"", [""jj@microsoft.com""]).value).maxMessageSize", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetRoomListsV2().value).address", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);
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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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

            IReadOnlyList<ConnectorFunction> fi = config.AddActionConnector("Office365Outlook", apiDoc, new ConsoleLogger(_output));
            Assert.Equal(97, fi.Count());

            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("Office365Outlook", apiDoc, new ConsoleLogger(_output));
            Assert.Equal(97, functions.Count());

            ConnectorFunction cf = functions.First(cf => cf.Name == "ContactPatchItemV2");

            // Validate required parameter name 
            Assert.Equal("id", cf.RequiredParameters[1].Name);

            // Validate optional parameter rectified name
            Assert.Equal("id_1", cf.OptionalParameters[0].Name);
        }

        [Fact]
        public async Task Office365Outlook_GetRooms()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Outlook.json");
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

            var xx = config.AddActionConnector(new ConnectorSettings("Office365Outlook") { AllowUnsupportedFunctions = true }, apiDoc, new ConsoleLogger(_output));
            var yy = xx[79].ReturnParameterType;

            var engine = new RecalcEngine(config);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("Office365Outlook", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetRooms.json");
            var result = await engine.EvalAsync(@"Office365Outlook.GetRoomsV2()", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);

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
        public async Task SQL_GetStoredProcs()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
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

            config.AddActionConnector("SQL", apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SQL", client, console: _output));

            testConnector.SetResponseFromFile(@"Responses\SQL Server GetProceduresV2.json");
            var result = await engine.EvalAsync(@"SQL.GetProceduresV2(""pfxdev-sql.database.windows.net"", ""connectortest"")", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc).ConfigureAwait(false);

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
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
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
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc).ConfigureAwait(false);

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
        public async Task SharePointOnlineTest()
        {            
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SharePoint.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXA...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "2f0cc19d-893e-e765-b15d-2906e3231c09" /* env */, "6fb0a1a8e2f5487eafbe306821d8377e" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions("SP", apiDoc, new ConsoleLogger(_output)).OrderBy(f => f.Name).ToList();
            Assert.Equal(101, functions.Count);

            IEnumerable<ConnectorFunction> funcInfos = config.AddActionConnector("SP", apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);
            RuntimeConfig rc = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext("SP", ppClient, console: _output));

            // -> https://auroraprojopsintegration01.sharepoint.com/sites/Site17
            testConnector.SetResponseFromFile(@"Responses\SPO_Response1.json");
            FormulaValue fv1 = await engine.EvalAsync(@$"SP.GetDataSets()", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            string dataset = ((StringValue)((TableValue)((RecordValue)fv1).GetField("value")).Rows.First().Value.GetField("Name")).Value;

            testConnector.SetResponseFromFile(@"Responses\SPO_Response2.json");
            FormulaValue fv2 = await engine.EvalAsync(@$"SP.GetDataSetsMetadata()", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            Assert.Equal("double", ((StringValue)((RecordValue)((RecordValue)fv2).GetField("blob")).GetField("urlEncoding")).Value);

            // -> 3756de7d-cb20-4014-bab8-6ea7e5264b97
            testConnector.SetResponseFromFile(@"Responses\SPO_Response3.json");
            FormulaValue fv3 = await engine.EvalAsync($@"SP.GetAllTables(""{dataset}"")", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            string table = ((StringValue)((TableValue)((RecordValue)fv3).GetField("value")).Rows.First().Value.GetField("Name")).Value;

            testConnector.SetResponseFromFile(@"Responses\SPO_Response4.json");
            FormulaValue fv4 = await engine.EvalAsync($@"SP.GetTableViews(""{dataset}"", ""{table}"")", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            Assert.Equal("1e54c4b5-2a59-4a2a-9633-cc611a2ff718", ((StringValue)((TableValue)fv4).Rows.Skip(1).First().Value.GetField("Name")).Value);

            testConnector.SetResponseFromFile(@"Responses\SPO_Response5.json");
            FormulaValue fv5 = await engine.EvalAsync($@"SP.GetItems(""{dataset}"", ""{table}"", {{'$top': 4}})", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            Assert.Equal("Shared Documents/Document.docx", ((StringValue)((RecordValue)((TableValue)((RecordValue)fv5).GetField("value")).Rows.First().Value).GetField("{FullPath}")).Value);

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
        public async Task ExcelOnlineTest()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\ExcelOnlineBusiness.swagger.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            string token = @"eyJ0eXAiOiJ...";

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient ppClient = new PowerPlatformConnectorClient("https://tip1-shared-002.azure-apim.net", "36897fc0-0c0c-eee5-ac94-e12765496c20" /* env */, "b20e87387f9149e884bdf0b0c87a67e8" /* connId */, () => $"{token}", httpClient) { SessionId = "547d471f-c04c-4c4a-b3af-337ab0637a0d" };

            ConnectorSettings connectorSettings = new ConnectorSettings("exob") { AllowUnsupportedFunctions = true };
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
            FormulaValue fv1 = await engine.EvalAsync(@$"exob.GetDrives(""{source}"")", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            string drive = ((StringValue)((TableValue)((RecordValue)fv1).GetField("value")).Rows.First((DValue<RecordValue> row) => ((StringValue)row.Value.GetField("name")).Value == "OneDrive").Value.GetField("id")).Value;

            // Get file id for "AM Site.xlxs" = "01UNLFRNUJPD7RJTFEMVBZZVLQIXHAKAOO"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response2.json");
            FormulaValue fv2 = await engine.EvalAsync(@$"exob.ListRootFolder(""{source}"", ""{drive}"")", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            string file = ((StringValue)((TableValue)fv2).Rows.First((DValue<RecordValue> row) => row.Value.GetField("Name") is StringValue sv && sv.Value == "AM Site.xlsx").Value.GetField("Id")).Value;

            // Get "Table1" id = "{00000000-000C-0000-FFFF-FFFF00000000}"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response3.json");
            FormulaValue fv3 = await engine.EvalAsync(@$"exob.GetTables(""{source}"", ""{drive}"", ""{file}"")", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            string table = ((StringValue)((TableValue)((RecordValue)fv3).GetField("value")).Rows.First((DValue<RecordValue> row) => ((StringValue)row.Value.GetField("name")).Value == "Table1").Value.GetField("id")).Value;

            // Get PowerApps id for 2nd row = "f830UPeAXoI"
            testConnector.SetResponseFromFile(@"Responses\EXO_Response4.json");
            FormulaValue fv4 = await engine.EvalAsync(@$"exob.GetItems(""{source}"", ""{drive}"", ""{file}"", ""{table}"")", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            string columnId = ((StringValue)((RecordValue)((TableValue)((RecordValue)fv4).GetField("value")).Rows.Skip(1).First().Value).GetField("__PowerAppsId__")).Value;

            // Get Item by columnId
            testConnector.SetResponseFromFile(@"Responses\EXO_Response5.json");
            FormulaValue fv5 = await engine.EvalAsync(@$"exob.GetItem(""{source}"", ""{drive}"", ""{file}"", ""{table}"", ""__PowerAppsId__"", ""{columnId}"")", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            RecordValue rv5 = (RecordValue)fv5;

            Assert.Equal(FormulaType.String, rv5.GetField("Site").Type);
            Assert.Equal("Atlanta", ((StringValue)rv5.GetField("Site")).Value);

            string version = PowerPlatformConnectorClient.Version;
            string expected = @$"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJ...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/36897fc0-0c0c-eee5-ac94-e12765496c20
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/excelonlinebusiness/b20e87387f9149e884bdf0b0c87a67e8/codeless/v1.0/drives?source=me
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJ...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/36897fc0-0c0c-eee5-ac94-e12765496c20
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/excelonlinebusiness/b20e87387f9149e884bdf0b0c87a67e8/codeless/v1.0/drives/b!kHbNLXp37U2hyy89eRtZD4Re_7zFnR1MsTMqs1_ocDwJW-sB0ZfqQ5NCc9L-sxKb/root/children?source=me
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJ...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/36897fc0-0c0c-eee5-ac94-e12765496c20
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/excelonlinebusiness/b20e87387f9149e884bdf0b0c87a67e8/codeless/v1.0/drives/b!kHbNLXp37U2hyy89eRtZD4Re_7zFnR1MsTMqs1_ocDwJW-sB0ZfqQ5NCc9L-sxKb/items/01UNLFRNUJPD7RJTFEMVBZZVLQIXHAKAOO/workbook/tables?source=me
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJ...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/36897fc0-0c0c-eee5-ac94-e12765496c20
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/excelonlinebusiness/b20e87387f9149e884bdf0b0c87a67e8/drives/b!kHbNLXp37U2hyy89eRtZD4Re_7zFnR1MsTMqs1_ocDwJW-sB0ZfqQ5NCc9L-sxKb/files/01UNLFRNUJPD7RJTFEMVBZZVLQIXHAKAOO/tables/%7b00000000-000C-0000-FFFF-FFFF00000000%7d/items?source=me
 x-ms-user-agent: PowerFx/{version}
POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXAiOiJ...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/36897fc0-0c0c-eee5-ac94-e12765496c20
 x-ms-client-session-id: 547d471f-c04c-4c4a-b3af-337ab0637a0d
 x-ms-request-method: GET
 x-ms-request-url: /apim/excelonlinebusiness/b20e87387f9149e884bdf0b0c87a67e8/drives/b!kHbNLXp37U2hyy89eRtZD4Re_7zFnR1MsTMqs1_ocDwJW-sB0ZfqQ5NCc9L-sxKb/files/01UNLFRNUJPD7RJTFEMVBZZVLQIXHAKAOO/tables/%7b00000000-000C-0000-FFFF-FFFF00000000%7d/items/f830UPeAXoI?source=me&idColumn=__PowerAppsId__
 x-ms-user-agent: PowerFx/{version}
";
            Assert.Equal(expected, testConnector._log.ToString());
        }
    }
}
