// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        [InlineData(4, 7, @"SQL.", "foo")]
        public void ConnectorIntellisenseTest(int responseIndex, int queryIndex, string expression, string expectedSuggestions)
        {
            // These tests are exercising 'x-ms-dynamic-values' extension property
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);

            using HttpClient httpClient = new HttpClient(); // testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared.azure-apim.net",           // endpoint 
                    "Default-91bee3d9-0c15-4f17-8624-c92bb8b36ead",     // environment
                    "06ce3808fed64cdc893c6dea9b7ce309",         // connectionId
                    () => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiJodHRwczovL2FwaWh1Yi5henVyZS5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MWJlZTNkOS0wYzE1LTRmMTctODYyNC1jOTJiYjhiMzZlYWQvIiwiaWF0IjoxNjg2MDUxMzU3LCJuYmYiOjE2ODYwNTEzNTcsImV4cCI6MTY4NjA1Njk4OSwiYWNyIjoiMSIsImFpbyI6IkFUUUF5LzhUQUFBQXdiNmRza1B2Z0ZiK0VvRFQ5ZG93MDVjM3dHRWJmeWI1M3lXck1nOXVKRnliVHd2NmJFd2toZTdkTTkxb0s5bC8iLCJhbXIiOlsicHdkIl0sImFwcGlkIjoiYThmN2E2NWMtZjViYS00ODU5LWIyZDYtZGY3NzJjMjY0ZTlkIiwiYXBwaWRhY3IiOiIwIiwiZmFtaWx5X25hbWUiOiJ1c2VyMTEiLCJnaXZlbl9uYW1lIjoiYXVyb3JhIiwiaXBhZGRyIjoiOTAuMTA0LjczLjIwMyIsIm5hbWUiOiJhdXJvcmF1c2VyMTEiLCJvaWQiOiI1YTY1MGQxZS03M2Q5LTQ4NDAtOGIyOS1lZWNmOGE3OGE1ZGQiLCJwdWlkIjoiMTAwMzIwMDEzQjlDODA1MyIsInJoIjoiMC5BVzhBMmVPLWtSVU1GMC1HSk1rcnVMTnVyVjg4QmY2U05oUlBydkx1TlB3SUhLNXZBRTQuIiwic2NwIjoiUnVudGltZS5BbGwiLCJzdWIiOiJaMWFmZENrVVp3WFBIUlZNcXlSci1ZbG1qMm5LQkpHdXRVTEdwTU00aWVzIiwidGlkIjoiOTFiZWUzZDktMGMxNS00ZjE3LTg2MjQtYzkyYmI4YjM2ZWFkIiwidW5pcXVlX25hbWUiOiJhdXJvcmF1c2VyMTFAY2FwaW50ZWdyYXRpb24wMS5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJhdXJvcmF1c2VyMTFAY2FwaW50ZWdyYXRpb24wMS5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiJoUmNNSDRZR1hFQzNUQkhTWlBrOEFBIiwidmVyIjoiMS4wIn0.O0S51NPFjUNz8g0HL0lVWjhw78CG9qCNrK1MDhxRrKJfLHyGUjy1p4diUoRXmK18twbRmJzMUQ06y4vXb5TLj-kDXYBq_RDGudbO2si_X2lXG-XKs2GM6qpqp-CsjTDUWRAnyHRkUfg8Pim10wj02N6mSFBpe0RbxrTahIMdHKhgfwkd1eQ_CdDNYB-GcIurbscDO7qJh8vqOoZfQbsyYTZ53-H09mq11cR41MMTfpzutKV6dy8SZo2qCGOjRRVi4cDGZzFOgSib0dHoNJv1GWwhqN_hYpIY8wKCB939Jh6LmgPY7zMvxTtL__5BiyApx33SV0547D77KLWqwYbzJQ",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            config.AddService("SQL", apiDoc, client);
            //testConnector.SetResponseFromFile(responseIndex switch
            //{
            //    0 => null,
            //    1 => @"Responses\SQL Server Intellisense Response 1.json",
            //    2 => @"Responses\SQL Server Intellisense Response 2.json",
            //    3 => @"Responses\SQL Server Intellisense Response 3.json",
            //    _ => null
            //});
            RecalcEngine engine = new RecalcEngine(config);

            CheckResult checkResult = engine.Check(expression, symbolTable: null);
            IIntellisenseResult suggestions = engine.Suggest(checkResult, expression.Length);

            string list = string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text).OrderBy(x => x));
            Assert.Equal(expectedSuggestions, list);
            //Assert.True((responseIndex == 0) ^ testConnector.SendAsyncCalled);

//            string networkTrace = testConnector._log.ToString();
//            string queryPart = queryIndex switch
//            {
//                0 => null,
//                1 => "servers",
//                2 => "databases?server=pfxdev-sql.database.windows.net",
//                3 => "databases?server=default",
//                4 => "v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures",
//                5 => "v2/datasets/default,connectortest/procedures",
//                6 => "v2/datasets/default,default/procedures",
//                _ => throw new NotImplementedException("Unknown index")
//            };

//            string expectedNetwork = queryIndex switch
//            {
//                0 => string.Empty,
//                _ =>
//$@"POST https://tip1-shared-002.azure-apim.net/invoke
// authority: tip1-shared-002.azure-apim.net
// Authorization: Bearer eyJ0eXA...
// path: /invoke
// scheme: https
// x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
// x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
// x-ms-request-method: GET
// x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/{queryPart}
// x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
//"
//            };

//            Assert.Equal(expectedNetwork.Replace("\r\n", "\n").Replace("\r", "\n"), networkTrace.Replace("\r\n", "\n").Replace("\r", "\n"));
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
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);

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

            Assert.Equal(expectedNetwork.Replace("\r\n", "\n").Replace("\r", "\n"), networkTrace.Replace("\r\n", "\n").Replace("\r", "\n"));
        }
    }
}

#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row
