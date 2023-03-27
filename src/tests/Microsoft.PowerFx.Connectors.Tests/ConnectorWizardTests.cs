// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

#pragma warning disable SA1119 // Statement should not use unnecessary parenthesis
#pragma warning disable SA1107 // Code should not contain multiple statements on one line

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class ConnectorWizardTests
    {
        [Fact]
        public void ConnectorWizardTest()
        {            
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            using HttpClient httpClient = new HttpClient();
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiJodHRwczovL2FwaWh1Yi5henVyZS5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MWJlZTNkOS0wYzE1LTRmMTctODYyNC1jOTJiYjhiMzZlYWQvIiwiaWF0IjoxNjc5OTMyMjQxLCJuYmYiOjE2Nzk5MzIyNDEsImV4cCI6MTY3OTkzNjg2OCwiYWNyIjoiMSIsImFpbyI6IkFUUUF5LzhUQUFBQWlmamw5blNnR3FTSllPZEFTMGdXd1U3NmxCbzlPcUJLdTJBUmQ5a1QzdWQxVElNelIvcllYOUVydWxWWjlFTnciLCJhbXIiOlsicHdkIl0sImFwcGlkIjoiYThmN2E2NWMtZjViYS00ODU5LWIyZDYtZGY3NzJjMjY0ZTlkIiwiYXBwaWRhY3IiOiIwIiwiZmFtaWx5X25hbWUiOiJTbWl0aCIsImdpdmVuX25hbWUiOiJFdmFuIiwiaXBhZGRyIjoiOTAuMTA0LjczLjIwMyIsIm5hbWUiOiJFdmFuIFNtaXRoIiwib2lkIjoiZTk4OTI4ZjAtMjQzOS00MzdkLTlmMGItNjgyYjU1YTIzMDUzIiwicHVpZCI6IjEwMDMyMDAwQkU4QjFEQUUiLCJyaCI6IjAuQVc4QTJlTy1rUlVNRjAtR0pNa3J1TE51clY4OEJmNlNOaFJQcnZMdU5Qd0lISzV2QUhvLiIsInNjcCI6IlJ1bnRpbWUuQWxsIiwic3ViIjoidmgyZUpyY0I1UWNOaElWNzNyeGlHU25HQVE2Z2hmd1J0enZBY2JnU2dRZyIsInRpZCI6IjkxYmVlM2Q5LTBjMTUtNGYxNy04NjI0LWM5MmJiOGIzNmVhZCIsInVuaXF1ZV9uYW1lIjoiYXVyb3JhdXNlcjA0QGNhcGludGVncmF0aW9uMDEub25taWNyb3NvZnQuY29tIiwidXBuIjoiYXVyb3JhdXNlcjA0QGNhcGludGVncmF0aW9uMDEub25taWNyb3NvZnQuY29tIiwidXRpIjoibEF6MllGb2Fra09fbFZSa2p2YzdBUSIsInZlciI6IjEuMCJ9.QbqN8i_Igh4KHvdFReuPu5T38yFfDa_zK8UD70tNwYaMltEoO795Wl0EDaZGBfs6OHonViVg-plwoyXqti_usqdDe7bfIPOnyI234-v4FEvHLADNnIS3JcgKRTljgGaBh3Tr_-MB1hC718LISZkribn_7epQOpU1HM4Ic-vWHrfSP3r5e2LP66mrMF_6kBr-6OItFJId2HbbqHzV1uVPzA0pgs09gWkHmazbApxSzU7w-FSJJ7_Q1HBoyi1u3bp1-T4D_PXgg3XeQytBw92CqQSgJltJRtZd9AG8ey-_mR3E6cP_athM5_2bCd05tKI9IkyeHzF9KC3dXLTQvK0XZA",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            OpenApiDocument apiDoc = testConnector._apiDocument;

            // Get all functions based on OpenApi document and using provided http client
            // throwOnError is set to true so that any later GetParameters call will generate an exception in case of HTTP failure (HTTP result not 200)
            // Default behavior: no exception and no suggestion in case of error
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(apiDoc, client, throwOnError: true);
            
            Assert.Equal(63, functions.Count());

            ConnectorFunction function = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            Assert.Equal("ExecuteProcedure_V2", function.OriginalName); // OperationId
            Assert.Equal(4, function.ArityMax);
            Assert.Equal(4, function.ArityMin);

            // Get list of parameters for ExecuteProcedureV2 function, without knowing any parameter
            // Notice that GetParameters does NOT validate parameter types
            ConnectorParameters parameters = function.GetParameters(Array.Empty<FormulaValue>());

            // We'll always get 4 parameters and some fields are constant (see CheckParameters)
            CheckParameters(parameters.Parameters);

            // GetParameters will retrieve the list of available SQL servers
            // We get the logical and display names
            // Notice that the display name isn't valid for an expression but pfxdev-sql.database.windows.net is working
            Assert.Equal(@"default|Use connection settings (pfxdev-sql.database.windows.net)", string.Join(", ", parameters.Parameters[0].Suggestions.Select(rv => $"{rv.GetField("Suggestion").ToObject()}|{rv.GetField("DisplayName").ToObject()}"))); // First parameter proposals            
            (1..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            // With first parameter defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default") });

            CheckParameters(parameters.Parameters);            

            // Now get the list of databases
            Assert.Equal(@"default|Use connection settings (connectortest)", string.Join(", ", parameters.Parameters[1].Suggestions.Select(rv => $"{rv.GetField("Suggestion").ToObject()}|{rv.GetField("DisplayName").ToObject()}"))); // Second parameter proposals
            (0, 2..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            // With two parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default") });

            CheckParameters(parameters.Parameters);

            // Get the list of stored procedures
            Assert.Equal(@"[dbo].[sp_1]|[dbo].[sp_1], [dbo].[sp_2]|[dbo].[sp_2]", string.Join(", ", parameters.Parameters[2].Suggestions.Select(rv => $"{rv.GetField("Suggestion").ToObject()}|{rv.GetField("DisplayName").ToObject()}"))); // Third parameter proposals
            Assert.Empty(parameters.Parameters[3].Suggestions);
            (0..1, 3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            // With three parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2") });

            CheckParameters(parameters.Parameters);
            (0..2).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));

            // Now the stored procedure name has been provided, we are getting the list of parameters, with their 'title' (display name) and 'type' (= SQL type)
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => s.Fields.First().Name))); // 4th parameter proposals
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => ((RecordValue)s.Fields.First().Value).GetField("title").ToObject().ToString())));
            Assert.Equal(@"integer, string", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => ((RecordValue)s.Fields.First().Value).GetField("type").ToObject().ToString())));
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            // With 4 parameters defined (and valid)
            // p1 type is not validated
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2"), FormulaValue.New(50) /* p1 */ });

            CheckParameters(parameters.Parameters);
            (0..2).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));

            // As 'p1' is provided, the only suggestion if 'p2'
            // Anyhow, the list of ParameterNames is still p1 and p2
            Assert.Equal(@"p2", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => s.Fields.First().Name)));
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            // With 5 parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2"), FormulaValue.New(50) /* p1 */, FormulaValue.New("abc") /* p2 */ });

            CheckParameters(parameters.Parameters);
            (0..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            Assert.True(parameters.IsCompleted);

            // Finally contruct the expression
            string expression = function.GetExpression("SQL", parameters);

            Assert.Equal(@"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1: 50, p2: ""abc"" })", expression);
        }

        private void CheckParameters(ConnectorParameterWithSuggestions[] parameters)
        {
            Assert.Equal(4, parameters.Length);

            Assert.Equal("Name of SQL server", parameters[0].Description);
            Assert.Equal("Server name", parameters[0].Summary);
            Assert.Equal("server", parameters[0].Name);
            Assert.Equal(FormulaType.String, parameters[0].FormulaType);

            Assert.Equal("Database name", parameters[1].Description);
            Assert.Equal("Database name", parameters[1].Summary);
            Assert.Equal("database", parameters[1].Name);
            Assert.Equal(FormulaType.String, parameters[1].FormulaType);

            Assert.Equal("Name of stored procedure", parameters[2].Description);
            Assert.Equal("Procedure name", parameters[2].Summary);
            Assert.Equal("procedure", parameters[2].Name);
            Assert.Equal(FormulaType.String, parameters[2].FormulaType);

            Assert.Equal("Body", parameters[3].Description);
            Assert.Equal("Parameters list", parameters[3].Summary);
            Assert.Equal("parameters", parameters[3].Name);
            Assert.Equal(RecordType.Empty(), parameters[3].FormulaType);
        }
    }

    public static class TestExtensions
    {
        public static void ForAll(this Range range, Action<int> action)
        {
            for (int i = range.Start.Value; i <= range.End.Value; i++)
            {
                action(i);
            }
        }

        public static void ForAll(this (Range range, int j) x, Action<int> action)
        {
            for (int i = x.range.Start.Value; i <= x.range.End.Value; i++)
            {
                action(i);
            }

            action(x.j);
        }

        public static void ForAll(this (int j, Range range) x, Action<int> action)
        {
            action(x.j);

            for (int i = x.range.Start.Value; i <= x.range.End.Value; i++)
            {
                action(i);
            }
        }
    }
}

#pragma warning restore SA1107 // Code should not contain multiple statements on one line
#pragma warning restore SA1119 // Statement should not use unnecessary parenthesis
