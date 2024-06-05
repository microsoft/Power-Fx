// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1119 // Statement should not use unnecessary parenthesis
#pragma warning disable SA1107 // Code should not contain multiple statements on one line

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class ConnectorWizardTests
    {
        private readonly ITestOutputHelper _output;

        public ConnectorWizardTests(ITestOutputHelper output)
        {
            _output = output;
        }

#if !NET462
        [Fact]
        public async Task ConnectorWizardTest()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
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
            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("SQL", client, console: _output);

            // Get all functions based on OpenApi document and using provided http client
            // throwOnError is set to true so that any later GetParameters call will generate an exception in case of HTTP failure (HTTP result not 200)
            // Default behavior: no exception and no suggestion in case of error
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(new ConnectorSettings("SQL") { IncludeInternalFunctions = true }, apiDoc, new ConsoleLogger(_output));

            Assert.Equal(64, functions.Count());

            ConnectorFunction executeProcedureV2 = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            Assert.Equal("ExecuteProcedure_V2", executeProcedureV2.OriginalName); // OperationId
            Assert.Equal("important", executeProcedureV2.Visibility);
            Assert.Equal(4, executeProcedureV2.ArityMax);
            Assert.Equal(4, executeProcedureV2.ArityMin);
            Assert.True(executeProcedureV2.IsSupported);
            Assert.True(string.IsNullOrEmpty(executeProcedureV2.NotSupportedReason));

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 1.json");

            // Get list of parameters for ExecuteProcedureV2 function, without knowing any parameter
            // Notice that GetParameters does NOT validate parameter types
            ConnectorParameters parameters = await executeProcedureV2.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), executeProcedureV2.RequiredParameters[0], context, CancellationToken.None);

            // We'll always get 4 parameters and some fields are constant (see CheckParameters)
            CheckParameters(parameters.ParametersWithSuggestions);

            // GetParameters will retrieve the list of available SQL servers
            // We get the logical and display names
            // Notice that the display name isn't valid for an expression but pfxdev-sql.database.windows.net is working
            Assert.Equal(@"default|Use connection settings (pfxdev-sql.database.windows.net)", string.Join(", ", parameters.ParametersWithSuggestions[0].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // First parameter proposals            
            (1..3).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.ParametersWithSuggestions[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 2.json");

            // With first parameter defined (and valid)
            parameters = await executeProcedureV2.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("server", FormulaValue.New(@"default")) }, executeProcedureV2.RequiredParameters[1], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions);

            // Now get the list of databases
            Assert.Equal(@"default|Use connection settings (connectortest)", string.Join(", ", parameters.ParametersWithSuggestions[1].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // Second parameter proposals
            (0, 2..3).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.ParametersWithSuggestions[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response 3.json");

            // With two parameters defined (and valid)
            parameters = await executeProcedureV2.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("server", FormulaValue.New(@"default")), new NamedValue("database", FormulaValue.New(@"default")) }, executeProcedureV2.RequiredParameters[2], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions);

            // Get the list of stored procedures
            Assert.Equal(@"[dbo].[sp_1]|[dbo].[sp_1], [dbo].[sp_2]|[dbo].[sp_2]", string.Join(", ", parameters.ParametersWithSuggestions[2].Suggestions.Select(rv => $"{rv.Suggestion.ToObject()}|{rv.DisplayName}"))); // Third parameter proposals
            Assert.Empty(parameters.ParametersWithSuggestions[3].Suggestions);
            (0..1, 3).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.ParametersWithSuggestions[i].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With three parameters defined (and valid)
            parameters = await executeProcedureV2.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("server", FormulaValue.New(@"default")), new NamedValue("database", FormulaValue.New(@"default")), new NamedValue("procedure", FormulaValue.New(@"sp_2")) }, executeProcedureV2.RequiredParameters[3], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions, true);
            (0..2).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));

            // Now the stored procedure name has been provided, we are getting the list of parameters, with their 'title' (display name) and 'type' (= SQL type)
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.ParametersWithSuggestions[3].Suggestions.Select(s => s.DisplayName))); // 4th parameter proposals
            Assert.Equal(@"Decimal, String", string.Join(", ", parameters.ParametersWithSuggestions[3].Suggestions.Select(s => ((BlankValue)s.Suggestion).Type.ToString())));
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.ParametersWithSuggestions[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With 4 parameters defined (and valid)
            // p1 is not specified here
            parameters = await executeProcedureV2.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("server", FormulaValue.New(@"default")), new NamedValue("database", FormulaValue.New(@"default")), new NamedValue("procedure", FormulaValue.New(@"sp_2")), new NamedValue("p1", FormulaValue.New(50)) }, executeProcedureV2.RequiredParameters[3], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions, true);
            (0..2).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));

            // As 'p1' is provided, the only suggestion if 'p2'
            // Anyhow, the list of ParameterNames is still p1 and p2
            Assert.Equal(@"p2", string.Join(", ", parameters.ParametersWithSuggestions[3].Suggestions.Select(s => s.DisplayName)));
            Assert.Equal(@"p1, p2", string.Join(", ", parameters.ParametersWithSuggestions[3].ParameterNames));
            Assert.False(parameters.IsCompleted);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Response2 2.json");

            // With 5 parameters defined (and valid)
            parameters = await executeProcedureV2.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("server", FormulaValue.New(@"default")), new NamedValue("database", FormulaValue.New(@"default")), new NamedValue("procedure", FormulaValue.New(@"sp_2")), new NamedValue("p1", FormulaValue.New(50)), new NamedValue("p2", FormulaValue.New("abc")) }, executeProcedureV2.RequiredParameters[3], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions, true);
            (0..3).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));
            Assert.True(parameters.IsCompleted);

            // Finally contruct the expression
            string expression = executeProcedureV2.GetExpression(parameters);

            Assert.Equal(@"SQL.ExecuteProcedureV2(""default"", ""default"", ""sp_2"", { p1: Decimal(50), p2: ""abc"" })", expression);
        }

        [Fact]
        public async Task ConnectorWizardTest_InvalidToken()
        {
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
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
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("SQL", apiDoc, new ConsoleLogger(_output));
            ConnectorFunction executeProcedureV2 = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("SQL", client, throwOnError: true, console: _output);

            // Simulates an invalid token
            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Error.json", System.Net.HttpStatusCode.BadRequest);
            await Assert.ThrowsAsync<HttpRequestException>(async () => await executeProcedureV2.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), executeProcedureV2.RequiredParameters[0], context, CancellationToken.None));

            // now let's try with throwOnError false
            functions = OpenApiParser.GetFunctions("SQL", apiDoc, new ConsoleLogger(_output));
            executeProcedureV2 = functions.First(cf => cf.Name == "ExecuteProcedureV2");

            // Same invalid token
            testConnector.SetResponseFromFile(@"Responses\SQL Server Intellisense Error.json", System.Net.HttpStatusCode.BadRequest);
            context = new TestConnectorRuntimeContext("SQL", client, throwOnError: false, console: _output);
            ConnectorParameters parameters = await executeProcedureV2.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), executeProcedureV2.RequiredParameters[0], context, CancellationToken.None);

            CheckParameters(parameters.ParametersWithSuggestions);

            // No suggestion           
            (0..3).ForAll(i => Assert.Empty(parameters.ParametersWithSuggestions[i].Suggestions));
            (0..3).ForAll(i => Assert.Null(parameters.ParametersWithSuggestions[i].ParameterNames));
            Assert.False(parameters.IsCompleted);
        }
#endif 

        /*
         * TEMPORARY REMOVAL, need to update 'SQL Server TestAllFunctions.jsonSet'
         * 
         
        [Fact]
        public async Task ConnectorWizardTest_TestAllFunctions()
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
            var functions = OpenApiParser.GetFunctions("SQL", apiDoc);
            testConnector.SetResponseSet(@"Responses\SQL Server TestAllFunctions.jsonSet");

            BaseRuntimeConnectorContext context = new TestConnectorRuntimeContext("SQL", client, throwOnError: true);

            foreach (ConnectorFunction function in functions)
            {
                ConnectorParameters parameters = await function.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), function.RequiredParameters.Any() ? function.RequiredParameters[0].Name : null, context, CancellationToken.None);
                Assert.NotNull(parameters);
            }
        }
        */

        private void CheckParameters(ConnectorParameterWithSuggestions[] parameters, bool paramaterTypeDetermined = false)
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

            Assert.Equal("Input parameters to the stored procedure", parameters[3].Description);
            Assert.Equal("Parameters list", parameters[3].Summary);
            Assert.Equal("parameters", parameters[3].Name);
            Assert.Equal(paramaterTypeDetermined ? @"![p1:w, p2:s]" : @"![]", parameters[3].FormulaType.ToStringWithDisplayNames());
        }
    }

#if !NET462
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
#endif
}

#pragma warning restore SA1107 // Code should not contain multiple statements on one line
#pragma warning restore SA1119 // Statement should not use unnecessary parenthesis
