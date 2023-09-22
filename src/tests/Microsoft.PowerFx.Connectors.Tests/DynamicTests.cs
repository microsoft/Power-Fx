// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class DynamicTests    
    {
        public const string Host = @"http://localhost:5189";
        public const string SwaggerFile = @"http://localhost:5189/swagger/v1/swagger.json";
        private static bool skip = false;

        private static void GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions)
        {
            functions = null;
            doc = null;

            try 
            {
                // lock managed by XUnit
                if (!skip)
                {
                    using WebClient webClient = new WebClient();
                    doc = new OpenApiStreamReader().Read(webClient.OpenRead(SwaggerFile), out var diagnostic);
                    functions = OpenApiParser.GetFunctions("Test", doc).OrderBy(cf => cf.Name).ToList();
                }
            }
            catch (WebException we)
            {
                // "No connection could be made because the target machine actively refused it."
                if (we.HResult == unchecked((int)0x80004005))
                {
                    skip = true;
                }
                else
                {
                    throw;
                }
            }

            if (skip)
            {
                Skip.If(true, "http://localhost:5189 test connector not available");
            }
        }

        private static void GetEngine(OpenApiDocument doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services)
        {
            HttpClient client = new HttpClient() { BaseAddress = new Uri(Host) };
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            config.AddActionConnector(new ConnectorSettings("Test"), doc);

            engine = new RecalcEngine(config);
            connectorContext = new TestConnectorRuntimeContext("Test", client, throwOnError: true);
            services = new BasicServiceProvider().AddRuntimeContext(connectorContext);
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStatic_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStatic = functions.First(cf => cf.Name == "GetWithDynamicValuesStatic");
            Assert.True(getWithDynamicValuesStatic.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStatic(";
            CheckResult result = engine.Check(expr, symbolTable: null);
            
            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("14|24", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStatic.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStatic.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("14|24", string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStatic_OptionalParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStaticOptional = functions.First(cf => cf.Name == "GetWithDynamicValuesStaticOptional");
            Assert.True(getWithDynamicValuesStaticOptional.OptionalParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStaticOptional({i: ";
            CheckResult result = engine.Check(expr, symbolTable: null);

            // Suggest API doesn't support optional parameters as it is based on 'argPosition' which is not available/meaningful for optional parameters
            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal(string.Empty, string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStaticOptional.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStaticOptional.OptionalParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("14|24", string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        // This is expected to work as ValueCollection has a default value defined as 'value'
        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStaticNoValueCollection_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStaticNoValueCollection = functions.First(cf => cf.Name == "GetWithDynamicValuesStaticNoValueCollection");
            Assert.True(getWithDynamicValuesStaticNoValueCollection.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStaticNoValueCollection(";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("14|24", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStaticNoValueCollection.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStaticNoValueCollection.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("14|24", string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStaticInvalidValueCollection_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStaticInvalidValueCollection = functions.First(cf => cf.Name == "GetWithDynamicValuesStaticInvalidValueCollection");
            Assert.True(getWithDynamicValuesStaticInvalidValueCollection.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStaticInvalidValueCollection(";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal(string.Empty, string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStaticInvalidValueCollection.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStaticInvalidValueCollection.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(string.Empty, string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStaticInvalidDisplayName_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStaticInvalidDisplayName = functions.First(cf => cf.Name == "GetWithDynamicValuesStaticInvalidDisplayName");
            Assert.True(getWithDynamicValuesStaticInvalidDisplayName.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStaticInvalidDisplayName(";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal(string.Empty, string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStaticInvalidDisplayName.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStaticInvalidDisplayName.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(string.Empty, string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesStaticInvalidValuePath_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesStaticInvalidValuePath = functions.First(cf => cf.Name == "GetWithDynamicValuesStaticInvalidValuePath");
            Assert.True(getWithDynamicValuesStaticInvalidValuePath.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesStaticInvalidValuePath(";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal(string.Empty, string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesStaticInvalidValuePath.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesStaticInvalidValuePath.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(string.Empty, string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesDynamic_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesDynamic = functions.First(cf => cf.Name == "GetWithDynamicValuesDynamic");
            Assert.True(getWithDynamicValuesDynamic.RequiredParameters[1].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesDynamic(5, ";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("15|25", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesDynamic.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("i", FormulaValue.New(5)) }, getWithDynamicValuesDynamic.RequiredParameters[1], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("15|25", string.Join("|", cParameters.ParametersWithSuggestions[1].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicValuesMultipleDynamic_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicValuesMultipleDynamic = functions.First(cf => cf.Name == "GetWithDynamicValuesMultipleDynamic");
            Assert.True(getWithDynamicValuesMultipleDynamic.RequiredParameters[2].SupportsDynamicIntellisense);
            Assert.Equal(Visibility.Important, getWithDynamicValuesMultipleDynamic.ConnectorReturnType.Fields[0].Fields[4].Visibility); // Array.index, inside RandomData schema

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicValuesMultipleDynamic(5, 7, ";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("50|60", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicValuesMultipleDynamic.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("i", FormulaValue.New(5)), new NamedValue("j", FormulaValue.New(7)) }, getWithDynamicValuesMultipleDynamic.RequiredParameters[2], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("50|60", string.Join("|", cParameters.ParametersWithSuggestions[2].Suggestions.Select(s => s.DisplayName)));

            // Missing parameter
            cParameters = await getWithDynamicValuesMultipleDynamic.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("i", FormulaValue.New(5)) }, getWithDynamicValuesMultipleDynamic.RequiredParameters[2], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(string.Empty, string.Join("|", cParameters.ParametersWithSuggestions[2].Suggestions.Select(s => s.DisplayName)));

            cParameters = await getWithDynamicValuesMultipleDynamic.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicValuesMultipleDynamic.OptionalParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("Database|Date|Index|Summary|SummaryEnum|TemperatureC|TemperatureF", string.Join("|", cParameters.ParametersWithSuggestions[3].Suggestions.Select(s => s.DisplayName)));
            Assert.Equal("![Database:![], Date:d, Index:w, Summary:s, SummaryEnum:w, TemperatureC:w, TemperatureF:w]", cParameters.ParametersWithSuggestions[3].FormulaType.ToStringWithDisplayNames());
            Assert.Equal(Visibility.Important, cParameters.ParametersWithSuggestions[3].ConnectorType.Fields[4].Visibility);

            ConnectorType cType = await getWithDynamicValuesMultipleDynamic.GetConnectorParameterTypeAsync(Array.Empty<NamedValue>(), getWithDynamicValuesMultipleDynamic.OptionalParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("![Database:![], Date:d, Index:w, Summary:s, SummaryEnum:w, TemperatureC:w, TemperatureF:w]", cType.FormulaType.ToStringWithDisplayNames());

            ConnectorType innerType = await getWithDynamicValuesMultipleDynamic.GetConnectorTypeAsync(Array.Empty<NamedValue>(), cType.Fields.First(field => field.Name == "Database"), connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("![Index:w, Name:s, PrimaryKey:s]", innerType.FormulaType.ToStringWithDisplayNames());

            ConnectorEnhancedSuggestions ces = await getWithDynamicValuesMultipleDynamic.GetConnectorSuggestionsAsync(Array.Empty<NamedValue>(), innerType.Fields.First(field => field.Name == "Index"), connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("110|120", string.Join("|", ces.ConnectorSuggestions.Suggestions.Select(cs => cs.DisplayName)));            
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicListStatic_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicListStatic = functions.First(cf => cf.Name == "GetWithDynamicListStatic");
            Assert.True(getWithDynamicListStatic.RequiredParameters[0].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicListStatic(";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("13|23", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicListStatic.GetParameterSuggestionsAsync(Array.Empty<NamedValue>(), getWithDynamicListStatic.RequiredParameters[0], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("13|23", string.Join("|", cParameters.ParametersWithSuggestions[0].Suggestions.Select(s => s.DisplayName)));
        }

        [SkippableFact]
        public async Task DynamicValues_GetWithDynamicListDynamic_RequiredParameters()
        {
            GetFunctionsInternal(out OpenApiDocument doc, out List<ConnectorFunction> functions);

            ConnectorFunction getWithDynamicListDynamic = functions.First(cf => cf.Name == "GetWithDynamicListDynamic");
            Assert.True(getWithDynamicListDynamic.RequiredParameters[1].SupportsDynamicIntellisense);

            GetEngine(doc, out RecalcEngine engine, out BaseRuntimeConnectorContext connectorContext, out BasicServiceProvider services);

            string expr = "Test.GetWithDynamicListDynamic(6, ";
            CheckResult result = engine.Check(expr, symbolTable: null);

            IIntellisenseResult suggestions = engine.Suggest(result, expr.Length, services);
            Assert.Equal("16|26", string.Join("|", suggestions.Suggestions.Select(s => s.DisplayText.Text)));

            ConnectorParameters cParameters = await getWithDynamicListDynamic.GetParameterSuggestionsAsync(new NamedValue[] { new NamedValue("i", FormulaValue.New(6)) }, getWithDynamicListDynamic.RequiredParameters[1], connectorContext, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("16|26", string.Join("|", cParameters.ParametersWithSuggestions[1].Suggestions.Select(s => s.DisplayName)));
        }
    }
}
