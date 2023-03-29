// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests;
using Xunit;

#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class IntellisenseTests
    {
        [Theory]
        // Get list of servers
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(", @"""default""")]
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(""", @"""default""")]     // inside the string, no character
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(""de", @"""default""")]   // inside the string, some characters (matching)
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(""dz", "")]               // inside the string, not matching characters
        // Get list of databases
        [InlineData(2, 2, @"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"",", @"""default""")]
        [InlineData(2, 3, @"SQL.ExecuteProcedureV2(""default"",", @"""default""")]                        // testing with "default" server
        [InlineData(0, 0, @"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net""", "")]             // no comma, still on 1st param
        [InlineData(0, 0, @"SQL.ExecuteProcedureV2(""pfxdev-sql"" + "".database.windows.net"",", "")]     // concatenation of strings
        // Get list of stored procedures
        [InlineData(3, 4, @"SQL.ExecuteProcedureV2(""pfxdev-sql.database.windows.net"", ""connectortest"",", @"""[dbo].[sp_1]""|""[dbo].[sp_2]""")]
        [InlineData(3, 5, @"SQL.ExecuteProcedureV2(""default"", ""connectortest"",", @"""[dbo].[sp_1]""|""[dbo].[sp_2]""")]       // testing with "default" server
        [InlineData(3, 6, @"SQL.ExecuteProcedureV2(""default"", ""default"",", @"""[dbo].[sp_1]""|""[dbo].[sp_2]""")]             // testing with "default" server & database
        public void ConnectorIntellisenseTest(int responseIndex, int queryIndex, string expression, string expectedSuggestions)
        {
            // These tests are exercising 'x-ms-dynamic-values' extension property
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig(Features.All);

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "eyJ0eXA...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddService("SQL", apiDoc, client);
            testConnector.SetResponseFromFile(responseIndex switch
            {
                0 => null,
                1 => @"Responses\SQL Server Intellisense Response 1.json",
                2 => @"Responses\SQL Server Intellisense Response 2.json",
                3 => @"Responses\SQL Server Intellisense Response 3.json",
                _ => null
            });
            RecalcEngine engine = new RecalcEngine(config);

            CheckResult checkResult = engine.Check(expression, symbolTable: null);
            IIntellisenseResult suggestions = engine.Suggest(checkResult, expression.Length);

            string list = string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text).OrderBy(x => x));
            Assert.Equal(expectedSuggestions, list);
            Assert.True((responseIndex == 0) ^ testConnector.SendAsyncCalled);

            string networkTrace = testConnector._log.ToString();
            string queryPart = queryIndex switch
            {
                0 => null,
                1 => "servers",
                2 => "databases?server=pfxdev-sql.database.windows.net",
                3 => "databases?server=default",
                4 => "v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures",
                5 => "v2/datasets/default,connectortest/procedures",
                6 => "v2/datasets/default,default/procedures",
                _ => throw new NotImplementedException("Unknown index")
            };

            string expectedNetwork = queryIndex switch
            {
                0 => string.Empty,
                _ =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/{queryPart}
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
"
            };

            Assert.Equal(expectedNetwork, networkTrace);
        }

        [Theory]        
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_1"", ", @"{ p1:")]        // stored proc with 1 param, out of record
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", ", @"{ p1:|{ p2:")]  // stored proc with 2 params, out of record
        [InlineData(1, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_1"", {  ", "p1")]         // in record, only suggest param names
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { ", "p1|p2")]       // two parameters
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p", @"p1|p2")]     // partial typing
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { q", @"")]          // unknown parameter
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1", @"p1")]       // fully typed
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1:", @"p1")]      // haven't started typing value
        [InlineData(2, 0, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1: 50", @"")]     // during value typing
        [InlineData(2, 1, @"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1: 50, ", @"p2")] // first param present, only propose missing params
        public void ConnectorIntellisenseTest2(int responseIndex, int networkCall, string expression, string expectedSuggestions)
        {
            // These tests are exercising 'x-ms-dynamic-schema' extension property
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig(Features.All);

            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "eyJ0eXA...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddService("SQL", apiDoc, client);
            if (networkCall > 0)
            {
                testConnector.SetResponseFromFile(responseIndex switch
                {
                    1 => @"Responses\SQL Server Intellisense Response2 1.json",
                    2 => @"Responses\SQL Server Intellisense Response2 2.json",
                    _ => null
                });
            }

            RecalcEngine engine = new RecalcEngine(config);

            CheckResult checkResult = engine.Check(expression, symbolTable: null);
            IIntellisenseResult suggestions = engine.Suggest(checkResult, expression.Length);

            string list = string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text).OrderBy(x => x));
            Assert.Equal(expectedSuggestions, list);            

            string networkTrace = testConnector._log.ToString();
            string expectedNetwork = networkCall == 0 ? string.Empty : 
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/$metadata.json/datasets/default,default/procedures/sp_{responseIndex}
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
";           

            Assert.Equal(expectedNetwork, networkTrace);
        }
    }
}

#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row
