// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Net.Http;
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
        // Exercise calling the MSNWeather connector against mocked Swagger and Response.json. 
        [Fact]
        public async Task MSNWeatherConnector_CurrentWeather()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json");
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net", // endpoint
                "839eace6-59ab-4243-97ec-a5b8fcc104e4", // environment
                "shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4", // connectionId
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
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MSNWeatherConnector_CurrentWeather_Error()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json");
            var apiDoc = testConnector._apiDocument;

            var config = new PowerFxConfig();

            using var httpClient = new HttpClient(); //testConnector);
            using var client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net", // endpoint
                "839eace6-59ab-4243-97ec-a5b8fcc104e4", // x-ms-client-environment-id
                "66c93435ddba4e88b5271c190fa503dc", // connectionId
                () => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiJodHRwczovL2FwaWh1Yi5henVyZS5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNjU5OTY2NTU4LCJuYmYiOjE2NTk5NjY1NTgsImV4cCI6MTY1OTk3MTU4MywiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQXNsSDNjclV6WWpXRmZ2RmVPZ3dybERsNEVGWkNMTUgzL3ZkYnZ3OEhYdm9aODUvZEFQMnFDbXVBWVFWQVlIQVNHcUJqVnNaakZQbGpKR3ZGMDA4UUdsWFlEQ0Jlc2RNVGdQWU1lK3dSSjYwPSIsImFtciI6WyJwd2QiLCJyc2EiLCJtZmEiXSwiYXBwaWQiOiJhOGY3YTY1Yy1mNWJhLTQ4NTktYjJkNi1kZjc3MmMyNjRlOWQiLCJhcHBpZGFjciI6IjAiLCJkZXZpY2VpZCI6IjJhMDUwN2E4LTk2N2ItNGM1YS04MDc0LWI4OWM0NTNjYTI0MCIsImZhbWlseV9uYW1lIjoiR2VuZXRpZXIiLCJnaXZlbl9uYW1lIjoiTHVjIiwiaXBhZGRyIjoiOTAuMTA0LjQzLjgzIiwibmFtZSI6Ikx1YyBHZW5ldGllciIsIm9pZCI6IjE1MDg3MTNiLThmY2ItNDk1MS05YWRkLWUxMWJiYmQ2MDJjMyIsIm9ucHJlbV9zaWQiOiJTLTEtNS0yMS0xNzIxMjU0NzYzLTQ2MjY5NTgwNi0xNTM4ODgyMjgxLTM3MjQ5IiwicHVpZCI6IjEwMDMzRkZGODAxQkRGQjgiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjE4OEJmNlNOaFJQcnZMdU5Qd0lISzRhQUw0LiIsInNjcCI6IlJ1bnRpbWUuQWxsIiwic3ViIjoidTJUaGU3NFRvU1JCLUZhT25ubDRoeWRTTTFobXVadW1Va2tLVnNfcTJZMCIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInVuaXF1ZV9uYW1lIjoibHVjZ2VuQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJsdWNnZW5AbWljcm9zb2Z0LmNvbSIsInV0aSI6ImllNVZWWm5lUEVTX2EtNUZEb0dWQUEiLCJ2ZXIiOiIxLjAifQ.br4MPsHhE3NW6yaedwv2sZb-6wLpbUsVgsAzdLfHRUretTEEDTkiydj4267qUopL5901ZjHufz6qaIy_tyjtazY2q-0mQ40E_tvUYE5e8z70yAS2sPVCT50P9wPw7PzC-XkU3alBanl8dZQqFFoSC6QctmXJygIi6Fpeqfkuuxf08m2v0R3ke7YKDcx5v71rG-QeCcaTM8LXfi_THyUZVFUous_3qRtiavJBwzmqdx2DcLd-00uT9IBmf8uxS9MTqAK6-g5miovouUFA2xtoOdLalVbVh-sUDyKZlHxReCCjObGNSj2rKXj0kO5b1qxeQkQjVu42ZRdgtrzh4PjIuA",
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
            //testConnector.SetResponseFromFile(@"Responses\MSNWeather_Response.json");

            var result = await engine.EvalAsync(
                "MSNWeather.CurrentWeather(\"Redmond\", \"Imperial\").responses.weather.current.temp",
                CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task AzureBlobConnector_UploadFile()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();
            var token = @"AuthToken2";

            using var httpClient = new HttpClient(testConnector);
            using var client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net",      // endpoint
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
            Assert.Equal(funcNames, new string[] { "AppendFile", "CopyFile", "CopyFile_Old", "CreateFile", "CreateFile_Old", "DeleteFile", "DeleteFile_Old", "ExtractFolder_Old", "ExtractFolderV2", "GetDataSetsMetadata", "GetFileContent", "GetFileContent_Old", "GetFileContentByPath", "GetFileContentByPath_Old", "GetFileMetadata", "GetFileMetadata_Old", "GetFileMetadataByPath", "GetFileMetadataByPath_Old", "ListAllRootFolders", "ListAllRootFoldersV2", "ListFolder", "ListFolder_Old", "ListFolderV2", "ListRootFolder", "ListRootFolder_Old", "ListRootFolderV2", "TestConnection", "UpdateFile", "UpdateFile_Old" });

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
            var expected =
@$"POST https://firstrelease-001.azure-apim.net/invoke
 authority: firstrelease-001.azure-apim.net
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

            Assert.Equal(expected, actual);
        }

        // Very documentation strings from the Swagger show up in the intellisense.
        [Fact]
        public void IntellisenseHelpStrings()
        {
            var apiDoc = Helpers.ReadSwagger(@"Swagger\MSNWeather.json");

            var config = new PowerFxConfig();
            config.AddService("MSNWeather", apiDoc, null);
            var engine = new Engine(config);

            var expr = "MSNWeather.CurrentWeather(";
            var result = engine.Suggest(expr, RecordType.Empty(), expr.Length);

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
    }
}
