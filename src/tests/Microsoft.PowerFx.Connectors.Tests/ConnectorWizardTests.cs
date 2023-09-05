// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Models;
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
            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "_eyJ0eXA...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            OpenApiDocument apiDoc = testConnector._apiDocument;
            BasicServiceProvider services = new BasicServiceProvider().AddService<RuntimeConnectorContext>(new TestConnectorRuntimeContext("SQL", client));

            // Get all functions based on OpenApi document and using provided http client
            // throwOnError is set to true so that any later GetParameters call will generate an exception in case of HTTP failure (HTTP result not 200)
            // Default behavior: no exception and no suggestion in case of error
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(new ConnectorSettings("SQL"), apiDoc);

            Assert.Equal(64, functions.Count());

            ConnectorFunction function = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            Assert.Equal("ExecuteProcedure_V2", function.OriginalName); // OperationId
            Assert.Equal("important", function.Visibility);
            Assert.Equal(4, function.ArityMax);
            Assert.Equal(4, function.ArityMin);
            Assert.True(function.IsSupported);
            Assert.True(string.IsNullOrEmpty(function.NotSupportedReason));

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 1.json");

            // Get list of parameters for ExecuteProcedureV2 function, without knowing any parameter
            // Notice that GetParameters does NOT validate parameter types
            ConnectorParameters parameters = function.GetParameters(Array.Empty<FormulaValue>(), services);

            // We'll always get 4 parameters and some fields are constant (see CheckParameters)
            CheckParameters(parameters.Parameters);

            // GetParameters will retrieve the list of available SQL servers
            // We get the logical and display names
            // Notice that the display name isn't valid for an expression but pfxdev-sql.database.windows.net is working
            Assert.Equal(@"default|Use connection settings (pfxdev-sql.database.windows.net)", string.Join(", ", parameters.Parameters[0].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // First parameter proposals            
            (1..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 2.json");

            // With first parameter defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default") }, services);

            CheckParameters(parameters.Parameters);

            // Now get the list of databases
            Assert.Equal(@"default|Use connection settings (connectortest)", string.Join(", ", parameters.Parameters[1].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // Second parameter proposals
            (0, 2..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 3.json");

            // With two parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default") }, services);

            CheckParameters(parameters.Parameters);

            // Get the list of stored procedures
            Assert.Equal(@"[dbo].[sp_1]|[dbo].[sp_1], [dbo].[sp_2]|[dbo].[sp_2]", string.Join(", ", parameters.Parameters[2].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // Third parameter proposals
            Assert.Empty(parameters.Parameters[3].Suggestions);
            (0..1, 3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With three parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2") }, services);

            CheckParameters(parameters.Parameters);
            (0..2).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));

            // Now the stored procedure name has been provided, we are getting the list of parameters, with their 'title' (display name) and 'type' (= SQL type)
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => s.DisplayName))); // 4th parameter proposals            ;
            Assert.Equal(@"Decimal, String", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => ((BlankValue)s.Suggestion).Type.ToString())));            
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With 4 parameters defined (and valid)
            // p1 type is not validated
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2"), FormulaValue.New(50) /* p1 */ }, services);

            CheckParameters(parameters.Parameters);
            (0..2).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));

            // As 'p1' is provided, the only suggestion if 'p2'
            // Anyhow, the list of ParameterNames is still p1 and p2
            Assert.Equal(@"p2", string.Join(", ", parameters.Parameters[3].Suggestions.Select(s => s.DisplayName)));
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.Parameters[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With 5 parameters defined (and valid)
            parameters = function.GetParameters(new FormulaValue[] { FormulaValue.New(@"default"), FormulaValue.New(@"default"), FormulaValue.New(@"sp_2"), FormulaValue.New(50) /* p1 */, FormulaValue.New("abc") /* p2 */ }, services);

            CheckParameters(parameters.Parameters);
            (0..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            Assert.True(parameters.IsCompleted);

            // Finally contruct the expression
            string expression = function.GetExpression("SQL", parameters);

            Assert.Equal(@"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1: Float(50), p2: ""abc"" })", expression);
        }

        [Fact]
        public void ConnectorWizardTest_InvalidToken()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "a2df3fb8-e4a4-e5e6-905c-e3dff9f93b46",     // environment
                    "5f57ec83acef477b8ccc769e52fa22cc",         // connectionId
                    () => "_eyJ0eXA...",
                    httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            OpenApiDocument apiDoc = testConnector._apiDocument;
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(new ConnectorSettings("SQL") { ThrowOnError = true }, apiDoc); //, throwOnError: true);
            ConnectorFunction function = functions.First(cf => cf.Name == "ExecuteProcedureV2");
            
            BasicServiceProvider services = new BasicServiceProvider().AddService<RuntimeConnectorContext>(new TestConnectorRuntimeContext("SQL", client));

            // Simulates an invalid token
            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Error.json", System.Net.HttpStatusCode.BadRequest);
            Assert.Throws<HttpRequestException>(() => function.GetParameters(Array.Empty<FormulaValue>(), services));

            // now let's try with throwOnError false
            functions = OpenApiParser.GetFunctions(new ConnectorSettings("SQL") { ThrowOnError = false }, apiDoc);
            function = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            // Same invalid token
            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Error.json", System.Net.HttpStatusCode.BadRequest);
            
            ConnectorParameters parameters = function.GetParameters(Array.Empty<FormulaValue>(), services);

            CheckParameters(parameters.Parameters);

            // No suggestion           
            (0..3).ForAll(i => Assert.Empty(parameters.Parameters[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.Parameters[i].ParameterNames));
            Assert.False(parameters.IsCompleted);
        }

        [Fact]
        public void ConnectorWizardTest_TestAllFunctions()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            using HttpClient httpClient = new HttpClient(testConnector);
            using PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(
                    "tip1-shared-002.azure-apim.net",           // endpoint 
                    "5edb9a6d-a246-e5e5-ad3c-957055a691ce",     // environment
                    "49f20efc201f4594bd99f971dd3a97d9",         // connectionId
                    () => "eyJ0eXAiO...",
                    httpClient)
            {
                SessionId = "c4d30b5e-b18d-425f-8fdc-0fd6939d42c7"
            };

            OpenApiDocument apiDoc = testConnector._apiDocument;
            var functions = OpenApiParser.GetFunctions(new ConnectorSettings("SQL") { ThrowOnError = true }, apiDoc);
            testConnector.SetResponseSet(@"Responses\SQL Server TestAllFunctions.jsonSet");
            
            BasicServiceProvider services = new BasicServiceProvider().AddService<RuntimeConnectorContext>(new TestConnectorRuntimeContext("SQL", client));

            foreach (ConnectorFunction function in functions)
            {
                ConnectorParameters parameters = function.GetParameters(Array.Empty<FormulaValue>(), services);
                Assert.NotNull(parameters);
            }
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
