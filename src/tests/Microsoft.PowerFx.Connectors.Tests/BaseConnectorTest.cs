// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.PowerFx.Tests.PowerPlatformConnectorTests;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public abstract class BaseConnectorTest : PowerFxTest, IDisposable
    {
        internal ITestOutputHelper _output;
        internal LoggingTestServer _testConnector;
        private bool _disposedValue;

        public BaseConnectorTest(ITestOutputHelper output, string swaggerFile)
        {
            _testConnector = new LoggingTestServer(swaggerFile);
            _output = output;
        }

        public abstract string GetNamespace();

        public abstract string GetEnvironment();

        public abstract string GetEndpoint();

        public abstract string GetConnectionId();

        public virtual ConnectorSettings GetConnectorSettings(ConnectorCompatibility compatibility = ConnectorCompatibility.PowerAppsCompatibility)
        {
            return new ConnectorSettings(GetNamespace()) { Compatibility = compatibility };
        }

        public virtual string GetJWTToken() => "Some JWT token";

        internal static string DisplaySuggestion(IntellisenseSuggestion s) => $"{(s.Kind == SuggestionKind.Global && s.Type.Kind > Core.Types.DKind.Unknown ? $"{s.DisplayText.Text}:{s.Type.Kind}" : s.DisplayText.Text)}";

        internal IReadOnlyList<ConnectorFunction> EnumerateFunctions()
        {
            (OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) = GetElements();
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));

            _output.WriteLine(string.Empty);
            foreach (ConnectorFunction func in funcs.OrderBy(f => f.Name))
            {
                _output.WriteLine(func.Name);
            }

            return funcs;
        }

        internal (OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) GetElements(bool live = false)
        {
            OpenApiDocument apiDoc = _testConnector._apiDocument;
            PowerFxConfig config = new PowerFxConfig();
            HttpClient httpClient = live ? new HttpClient() : new HttpClient(_testConnector); // With _testConnector, we can mock the response

            PowerPlatformConnectorClient client = new PowerPlatformConnectorClient(GetEndpoint(), GetEnvironment(), GetConnectionId(), () => GetJWTToken(), httpClient)
            {
                SessionId = "9315f316-5182-4260-b333-7a43a36ca3b0" // any GUID will work
            };

            ConnectorSettings connectorSettings = GetConnectorSettings();

            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(new TestConnectorRuntimeContext(GetNamespace(), client, console: _output));
            runtimeConfig.SetClock(new TestClockService());
            runtimeConfig.SetTimeZone(TimeZoneInfo.Utc);

            return (apiDoc, config, httpClient, client, connectorSettings, runtimeConfig);
        }

        internal async Task RunConnectorTestAsync(bool live, string expr, string expectedResult, string xUrls, string xBodies, string[] expectedFiles, bool displayIntellisenseResults)
        {
            _output.WriteLine($"EXPR: {expr}");
            _output.WriteLine(string.Empty);

            (OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) = GetElements(live);
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));

            RecalcEngine engine = new RecalcEngine(config);
            if (!live)
            {
                _testConnector.SetResponseFromFiles(expectedFiles.Select(ef =>
                    ef[3] != ':' // status code specified
                    ? ($@"Responses\{ef}", HttpStatusCode.OK)
                    : string.IsNullOrEmpty(ef.Substring(4)) // no file specified
                    ? (null, (HttpStatusCode)int.Parse(ef.Substring(0, 3)))
                    : ($@"Responses\{ef.Substring(4)}", (HttpStatusCode)int.Parse(ef.Substring(0, 3)))).ToArray());
            }

            FormulaValue fv = await engine.EvalAsync(expr, CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            if (string.IsNullOrEmpty(expectedResult))
            {
                Assert.IsType<BlankValue>(fv);
            }
            else if (expectedResult.StartsWith("RECORD"))
            {
                Assert.IsAssignableFrom<RecordValue>(fv);
            }
            else if (expectedResult == "IMAGE")
            {
                // Do nothing for now
            }
            else if (expectedResult.StartsWith("ERR:"))
            {
                ErrorValue ev = Assert.IsType<ErrorValue>(fv);
                string err = string.Join(",", ev.Errors.Select(er => er.Message));

                foreach (string er in expectedResult.Substring(4).Split("|"))
                {
                    Assert.Contains(er, err);
                }
            }
            else
            {
                Assert.True(fv is not ErrorValue, fv is ErrorValue ev ? $"EvalAsync Error: {string.Join(", ", ev.Errors.Select(er => er.Message))}" : null);
                StringValue sv = Assert.IsType<StringValue>(fv);

                Assert.Equal(expectedResult, sv.Value);
            }

            if (!live)
            {
                string network = _testConnector._log.ToString();
                string urls = string.Join("|", Regex.Matches(network, @"x-ms-request-method: (?<r>[^ \r\n]+)\s*x-ms-request-url: (?<u>[^ \r\n]+)").Select(g => $"{g.Groups["r"].Value}:{g.Groups["u"].Value}"));                
                string bodies = string.Join("|", Regex.Matches(network, @"\[body\] (?<b>.*)").Select(g => g.Groups["b"].Value.Replace("\r", string.Empty).Replace("\n", string.Empty)));

                _output.WriteLine(string.Empty);
                _output.WriteLine(network);

                Assert.Equal(xUrls, urls);
                Assert.Equal(xBodies, bodies);
            }

            if (displayIntellisenseResults)
            {
                for (int i = 1; i < expr.Length; i++)
                {
                    string e = expr.Substring(0, i);
                    CheckResult cr = engine.Check(e, new ParserOptions() { AllowsSideEffects = true }, null);

                    IIntellisenseResult iir = engine.Suggest(cr, i);
                    IEnumerable<IIntellisenseSuggestion> suggestions = iir.Suggestions.Any() ? iir.Suggestions : iir.FunctionOverloads;
                    string suggestionsStr = string.Join("|", suggestions.Cast<IntellisenseSuggestion>().Select(s => DisplaySuggestion(s)));

                    _output.WriteLine(string.Empty);
                    _output.WriteLine($"Expression: {e}");
                    _output.WriteLine(suggestionsStr);
                    _output.WriteLine("  >" + string.Join("\r\n  >", iir.Suggestions.Select(s => s.DisplayText.Text)));
                }
            }
        }

        internal void RunIntellisenseTest(string expr, string expectedSuggestions)
        {
            _output.WriteLine($"EXPR: {expr}");
            _output.WriteLine(string.Empty);

            (OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) = GetElements();
            config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));

            RecalcEngine engine = new RecalcEngine(config);
            CheckResult cr = engine.Check(expr, new ParserOptions() { AllowsSideEffects = true }, null);

            IIntellisenseResult iir = engine.Suggest(cr, expr.Length);
            IEnumerable<IIntellisenseSuggestion> suggestions = iir.Suggestions.Any() ? iir.Suggestions : iir.FunctionOverloads;
            string suggestionsStr = string.Join("|", suggestions.Cast<IntellisenseSuggestion>().Select(s => DisplaySuggestion(s)));

            Assert.Equal(expectedSuggestions, suggestionsStr);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _testConnector?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
