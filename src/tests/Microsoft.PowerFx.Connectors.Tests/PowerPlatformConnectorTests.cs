// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Tests
{
    // Simulate calling PowerPlatform connectors.
    public class PowerPlatformConnectorTests : PowerFxTest
    {
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

            var funcs = config.AddService("MSNWeather", apiDoc, client);

            // Function we added where specified in MSNWeather.json
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();
            Assert.Equal(funcNames, new string[] { "CurrentWeather", "GetMeasureUnits", "TodaysForecast", "TomorrowsForecast" });

            // Now execute it...
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\MSNWeather_Response.json");

            var result = await engine.EvalAsync(
                "MSNWeather.CurrentWeather(\"Redmond\", \"Imperial\").responses.weather.current.temp",
                CancellationToken.None);

            Assert.Equal(53.0, result.ToObject()); // from response

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

            var funcs = config.AddService("TestConnector12", apiDoc, client);

            // Now execute it...
            var engine = new RecalcEngine(config);
            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result = await engine.EvalAsync($"TestConnector12.GenerateError({{error: {statusCode}}})", CancellationToken.None);

            Assert.NotNull(result);

            if (statusCode < 300)
            {
                Assert.IsType<NumberValue>(result);

                var nv = (NumberValue)result;

                Assert.Equal(statusCode, nv.Value);
            }
            else
            {
                Assert.IsType<ErrorValue>(result);

                var ev = (ErrorValue)result;

                Assert.Equal(FormulaType.Number, ev.Type);
                Assert.Equal(1, ev.Errors.Count);

                var err = ev.Errors[0];

                Assert.Equal(ErrorKind.Network, err.Kind);
                Assert.Equal(ErrorSeverity.Critical, err.Severity);
                Assert.Equal($"TestConnector12.GenerateError failed: The server returned an HTTP error with code {statusCode}. Response: {statusCode}", err.Message);
            }

            testConnector.SetResponse($"{statusCode}", (HttpStatusCode)statusCode);
            var result2 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),FirstError.Message)", CancellationToken.None);

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
            var result3 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),CountRows(AllErrors))", CancellationToken.None);

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
            var token = @"AuthToken2";

            using var httpClient = new HttpClient(testConnector);
            using var client = useSwaggerParameter ?
                new PowerPlatformConnectorClient(
                    apiDoc,                                 // Swagger file
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4", // environment
                    "453f61fa88434d42addb987063b1d7d2",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                }
                : new PowerPlatformConnectorClient(
                    (useHttpsPrefix ? "https://" : string.Empty) +
                        "firstrelease-001.azure-apim.net",  // endpoint
                    "839eace6-59ab-4243-97ec-a5b8fcc104e4", // environment
                    "453f61fa88434d42addb987063b1d7d2",     // connectionId
                    () => $"{token}",
                    httpClient)
                {
                    SessionId = "ccccbff3-9d2c-44b2-bee6-cf24aab10b7e"
                };

            var funcs = config.AddService("AzureBlobStorage", apiDoc, client);

            // Function we added where specified in MSNWeather.json
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();
            Assert.Equal(funcNames, new string[] { "AppendFile", "CopyFile", "CopyFileOld", "CreateFile", "CreateFileOld", "DeleteFile", "DeleteFileOld", "ExtractFolderOld", "ExtractFolderV2", "GetDataSetsMetadata", "GetFileContent", "GetFileContentByPath", "GetFileContentByPathOld", "GetFileContentOld", "GetFileMetadata", "GetFileMetadataByPath", "GetFileMetadataByPathOld", "GetFileMetadataOld", "ListAllRootFolders", "ListAllRootFoldersV2", "ListFolder", "ListFolderOld", "ListFolderV2", "ListRootFolder", "ListRootFolderOld", "ListRootFolderV2", "TestConnection", "UpdateFile", "UpdateFileOld" });

            // Now execute it...
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_Response.json");

            var result = await engine.EvalAsync(
                @"AzureBlobStorage.CreateFile(""container"", ""bora1.txt"", ""abc"").Size",
                CancellationToken.None,
                options: new ParserOptions() { AllowsSideEffects = true });

            dynamic res = result.ToObject();
            var size = (double)res;

            Assert.Equal(3.0, size);

            // PowerPlatform Connectors transform the request significantly from what was in the swagger. 
            // Some of this information comes from setting passed into connector client. 
            // Other information is from swagger. 
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var host = useSwaggerParameter ? "tip1-shared.azure-apim.net" : "firstrelease-001.azure-apim.net";
            var expected =
@$"POST https://{host}/invoke
 authority: {host}
 Authorization: Bearer {token}
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/839eace6-59ab-4243-97ec-a5b8fcc104e4
 x-ms-client-session-id: ccccbff3-9d2c-44b2-bee6-cf24aab10b7e
 x-ms-request-method: POST
 x-ms-request-url: /apim/azureblob/453f61fa88434d42addb987063b1d7d2/datasets/default/files?folderPath=container&name=bora1.txt
 x-ms-user-agent: PowerFx/{version}
 [content-header] Content-Type: text/plain; charset=utf-8
 [body] abc
";

            AssertEqual(expected, actual);
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
            config.AddService("MSNWeather", apiDoc, null);
            config.AddBehaviorFunction();

            var engine = new Engine(config);
            var check = engine.Check(expr, RecordType.Empty(), withAllowSideEffects ? new ParserOptions() { AllowsSideEffects = true } : null);

            if (expectedBehaviorError)
            {
                Assert.Contains(check.Errors, d => d.Message == Extensions.GetErrBehaviorPropertyExpectedMessage());
            }
            else
            {
                Assert.DoesNotContain(check.Errors, d => d.Message == Extensions.GetErrBehaviorPropertyExpectedMessage());
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
            Assert.Equal("tip1-shared.azure-apim.net", ppcl3.Endpoint);

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
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Swagger.json");
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

            config.AddService("Office365Users", apiDoc, client);           
            var engine = new RecalcEngine(config);            
            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");            
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_MyProfile_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Swagger.json");
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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.MyProfileV2().mobilePhone", CancellationToken.None);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_DirectReports_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Swagger.json");
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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_DirectsV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.DirectReportsV2(""jmstall@microsoft.com"", {'$top': 4 }).value).city", CancellationToken.None);

            Assert.IsType<StringValue>(result);
            Assert.Equal("Paris", (result as StringValue).Value);
        }

        [Fact]
        public async Task Office365Users_SearchUsers_V2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Swagger.json");
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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_SearchV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.SearchUserV2({searchTerm:""Doe"", top: 3}).value).DisplayName", CancellationToken.None);

            Assert.IsType<StringValue>(result);
            Assert.Equal("John Doe", (result as StringValue).Value);
        }
    }
}
