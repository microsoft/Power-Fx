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
            string expectedNetwork = queryIndex switch
            {
                0 => string.Empty,
                1 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/servers
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                2 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/databases?server=pfxdev-sql.database.windows.net
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                3 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/databases?server=default
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                4 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/pfxdev-sql.database.windows.net,connectortest/procedures
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                5 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/default,connectortest/procedures
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                6 =>
$@"POST https://tip1-shared-002.azure-apim.net/invoke
 authority: tip1-shared-002.azure-apim.net
 Authorization: Bearer eyJ0eXA...
 path: /invoke
 scheme: https
 x-ms-client-environment-id: /providers/Microsoft.PowerApps/environments/a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46
 x-ms-client-session-id: 8e67ebdc-d402-455a-b33a-304820832383
 x-ms-request-method: GET
 x-ms-request-url: /apim/sql/5f57ec83acef477b8ccc769e52fa22cc/v2/datasets/default,default/procedures
 x-ms-user-agent: PowerFx/{PowerPlatformConnectorClient.Version}
",
                _ => throw new NotImplementedException("Unknown index")
            };

            Assert.Equal(expectedNetwork, networkTrace);
        }
    }
}

#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row
