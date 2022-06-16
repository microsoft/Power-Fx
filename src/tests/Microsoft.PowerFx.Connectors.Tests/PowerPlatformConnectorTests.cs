// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
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
            var testConnector = new LoggingTestServer(@"Swagger\MSNWeather.json");
            var apiDoc = testConnector._apiDocument;

            var config = new PowerFxConfig();

            HttpClient client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net", // endpoint
                "839eace6-59ab-4243-97ec-a5b8fcc104e4", // environment
                "shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4", // connectionId
                () => "AuthToken1",
                new HttpClient(testConnector))
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
        public async Task AzureBlobConnector_UploadFile()
        {
            var testConnector = new LoggingTestServer(@"Swagger\AzureBlobStorage.json");
            var apiDoc = testConnector._apiDocument;                       
            var config = new PowerFxConfig();
            var token = @"AuthToken2";

            HttpClient client = new PowerPlatformConnectorClient(
                "firstrelease-001.azure-apim.net",      // endpoint
                "839eace6-59ab-4243-97ec-a5b8fcc104e4", // environment
                "453f61fa88434d42addb987063b1d7d2",     // connectionId
                () => $"{token}",
                new HttpClient(testConnector))
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
 [header] Content-Type: text/plain; charset=utf-8
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
            var result = engine.Suggest(expr, new RecordType(), expr.Length);

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
