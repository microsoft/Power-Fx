﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

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

            var result = await engine.EvalAsync("MSNWeather.CurrentWeather(\"Redmond\", \"Imperial\").responses.weather.current.temp", CancellationToken.None).ConfigureAwait(false);
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
            var result = await engine.EvalAsync($"TestConnector12.GenerateError({{error: {statusCode}}})", CancellationToken.None).ConfigureAwait(false);

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
            var result2 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),FirstError.Message)", CancellationToken.None).ConfigureAwait(false);

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
            var result3 = await engine.EvalAsync($"IfError(Text(TestConnector12.GenerateError({{error: {statusCode}}})),CountRows(AllErrors))", CancellationToken.None).ConfigureAwait(false);

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

            var funcs = config.AddService("AzureBlobStorage", apiDoc, client);            
            var funcNames = funcs.Select(func => func.Name).OrderBy(x => x).ToArray();
            Assert.Equal(funcNames, new string[] { "AppendFile", "CopyFile", "CopyFileOld", "CreateFile", "CreateFileOld", "DeleteFile", "DeleteFileOld", "ExtractFolderOld", "ExtractFolderV2", "GetDataSetsMetadata", "GetFileContent", "GetFileContentByPath", "GetFileContentByPathOld", "GetFileContentOld", "GetFileMetadata", "GetFileMetadataByPath", "GetFileMetadataByPathOld", "GetFileMetadataOld", "ListAllRootFolders", "ListAllRootFoldersV2", "ListFolder", "ListFolderOld", "ListFolderV2", "ListRootFolder", "ListRootFolderOld", "ListRootFolderV2", "TestConnection", "UpdateFile", "UpdateFileOld" });

            // Now execute it...
            var engine = new RecalcEngine(config);                        
            testConnector.SetResponseFromFile(@"Responses\AzureBlobStorage_Response.json");

            var result = await engine.EvalAsync(@"AzureBlobStorage.CreateFile(""container"", ""bora4.txt"", ""abc"").Size", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }).ConfigureAwait(false);

            dynamic res = result.ToObject();
            var size = (double)res;

            Assert.Equal(3.0, size);

            // PowerPlatform Connectors transform the request significantly from what was in the swagger. 
            // Some of this information comes from setting passed into connector client. 
            // Other information is from swagger. 
            var actual = testConnector._log.ToString();

            var version = PowerPlatformConnectorClient.Version;
            var host = useSwaggerParameter ? "tip1-shared.azure-apim.net" : "tip1-shared-002.azure-apim.net";
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

            config.AddService("Office365Users", apiDoc, client);           
            var engine = new RecalcEngine(config);            
            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");            
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None).ConfigureAwait(false);

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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.MyProfileV2().mobilePhone", CancellationToken.None).ConfigureAwait(false);

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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_DirectsV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.DirectReportsV2(""jmstall@microsoft.com"", {'$top': 4 }).value).city", CancellationToken.None).ConfigureAwait(false);

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

            config.AddService("Office365Users", apiDoc, client);
            var engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office365_SearchV2.json");
            var result = await engine.EvalAsync(@"First(Office365Users.SearchUserV2({searchTerm:""Doe"", top: 3}).value).DisplayName", CancellationToken.None).ConfigureAwait(false);

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

            config.AddService("Office365Outlook", apiDoc, client);
            RecalcEngine engine = new RecalcEngine(config);
            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetEmails.json");
            FormulaValue result = await engine.EvalAsync(@"First(Office365Outlook.GetEmails({ top : 5 })).DateTimeReceived", CancellationToken.None).ConfigureAwait(false);

            Assert.IsType<DateTimeValue>(result);

            // Convert to UTC so that we don't depend on local machine settings
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc((result as DateTimeValue).GetConvertedValue(null)); // null = no conversion

            Assert.Equal(new DateTime(2023, 6, 6, 5, 4, 59, DateTimeKind.Utc), utcTime);
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

            IReadOnlyList<FunctionInfo> fi = config.AddService("Office365Outlook", apiDoc, client);
            Assert.Equal(107, fi.Count());
            
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(apiDoc);
            Assert.Equal(107, functions.Count());

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

            config.AddService("Office365Outlook", apiDoc, client);
            var engine = new RecalcEngine(config);

            testConnector.SetResponseFromFile(@"Responses\Office 365 Outlook GetRooms.json");
            var result = await engine.EvalAsync(@"Office365Outlook.GetRoomsV2()", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }).ConfigureAwait(false);

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

            config.AddService("SQL", apiDoc, client);
            var engine = new RecalcEngine(config);

            testConnector.SetResponseFromFile(@"Responses\SQL Server GetProceduresV2.json");
            var result = await engine.EvalAsync(@"SQL.GetProceduresV2(""pfxdev-sql.database.windows.net"", ""connectortest"")", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }).ConfigureAwait(false);

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

            config.AddService("SQL", apiDoc, client);
            var engine = new RecalcEngine(config);

            testConnector.SetResponseFromFile(@"Responses\SQL Server ExecuteStoredProcedureV2.json");
            FormulaValue result = await engine.EvalAsync(@"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"", ""sp_1"", { p1: 50 })", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }).ConfigureAwait(false);

            Assert.Equal(FormulaType.UntypedObject, result.Type);

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
    }
}
