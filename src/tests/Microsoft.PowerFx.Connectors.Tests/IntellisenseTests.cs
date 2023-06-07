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
        public void ConnectorIntellisenseTest(int responseIndex, int queryIndex, string expression, string expectedSuggestions)
        {
            // These tests are exercising 'x-ms-dynamic-values' extension property
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            OpenApiDocument apiDoc = testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);

            using HttpClient httpClient = new HttpClient(); // testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",                    // endpoint 
                    "3c622ede-264e-e91c-b8fa-28210aff12dc",  // environment
                    "52d3dd1962c44b7b93aed903d02e0b22",              // connectionId
                    () => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiJodHRwczovL2FwaWh1Yi5henVyZS5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNjg2MTM3MTQ2LCJuYmYiOjE2ODYxMzcxNDYsImV4cCI6MTY4NjE0MjY1MywiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQUJ3amo3am9vVGpUUHdCRTNYS0NsYXMrdmx4TkpjZ0xIWktLeE4vdGdwbXBnYU44ZEwyYVZWRVFnclFETWY5MFZGZFRSOFB0alpGT3k4NmRoTnhpM2NLQ0FTejN4OGIyYjQ5TUd2VnVmRW5zPSIsImFtciI6WyJwd2QiLCJyc2EiLCJtZmEiXSwiYXBwaWQiOiJhOGY3YTY1Yy1mNWJhLTQ4NTktYjJkNi1kZjc3MmMyNjRlOWQiLCJhcHBpZGFjciI6IjAiLCJkZXZpY2VpZCI6IjJhMDUwN2E4LTk2N2ItNGM1YS04MDc0LWI4OWM0NTNjYTI0MCIsImZhbWlseV9uYW1lIjoiR2VuZXRpZXIiLCJnaXZlbl9uYW1lIjoiTHVjIiwiaXBhZGRyIjoiOTAuMTA0LjczLjIwMyIsIm5hbWUiOiJMdWMgR2VuZXRpZXIiLCJvaWQiOiIxNTA4NzEzYi04ZmNiLTQ5NTEtOWFkZC1lMTFiYmJkNjAyYzMiLCJvbnByZW1fc2lkIjoiUy0xLTUtMjEtMTcyMTI1NDc2My00NjI2OTU4MDYtMTUzODg4MjI4MS0zNzI0OSIsInB1aWQiOiIxMDAzM0ZGRjgwMUJERkI4IiwicmgiOiIwLkFSb0F2NGo1Y3ZHR3IwR1JxeTE4MEJIYlIxODhCZjZTTmhSUHJ2THVOUHdJSEs0YUFMNC4iLCJzY3AiOiJSdW50aW1lLkFsbCIsInN1YiI6InUyVGhlNzRUb1NSQi1GYU9ubmw0aHlkU00xaG11WnVtVWtrS1ZzX3EyWTAiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6Imx1Y2dlbkBtaWNyb3NvZnQuY29tIiwidXBuIjoibHVjZ2VuQG1pY3Jvc29mdC5jb20iLCJ1dGkiOiJ0SW1lMDhVbkwweVU0eEpsQkxaMEFBIiwidmVyIjoiMS4wIn0.sney5nQSCjfhOmFSfVX9AQdV0XxGHD8PBXlwpCb2lb-ntxJ-s-Jg1S5rFTE0on16bL8-PkIiMUTI3nE0qhXvC6c17tX9vCMdTFelmq_r42nVJsNXiRGiIyR8sOEnO4X-qttWczZIgT_Yd73vJwKgMqsuuiA1k1e1LxiMBZq2Uztc5E17iIZWX0AdrFCCEh0NrIi6Gcf_QsB_cANQGCf-In6MmI8un8l_9X06scOAIoAo8kRRxxZnmH1v7O-f3-PuHbIoVJBlJmjNETte6LF_2IyET0n8zwmqLkZ9ODr7N3xHjeYyT2YzmWCajwstJ6-Ky71AempcIq5aE5IDXu0bvQ",
                    httpClient)
            {
                SessionId = "fcf636e8-5b65-40c8-95ad-d75051cd97b1"
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
