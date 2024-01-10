// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class PowerAppsForMakersTests : BaseConnectorTest
    {
        public PowerAppsForMakersTests(ITestOutputHelper output)
            : base(output, @"Swagger\PowerAppsForMakers.json")
        {
        }

        public override string GetNamespace() => "PowerAppsForMakers";

        public override string GetEnvironment() => "a323f485-54aa-e556-937d-86727a0c5ac0";

        public override string GetEndpoint() => "tip1002-002.azure-apihub.net";

        public override string GetConnectionId() => "763ee25d4f4046debbfcddcf9175db97";        

        [Fact]
        public void PowerAppsForMakers_EnumFuncs()
        {
            EnumerateFunctions();
        }

        /*
            EditAppRoleAssignment
            EditConnectionRoleAssignment
            EditConnectorRoleAssignment
            GetApp
            GetAppRoleAssignment
            GetApps
            GetAppVersions
            GetConnectionRoleAssignment
            GetConnections
            GetConnector
            GetConnectorRoleAssignment
            GetConnectors
            GetEnvironments
            PublishApp
            RemoveApp
            RemoveConnection
            RemoveConnector
            RestoreAppVersion
            SetAppDisplayName
        */

        // To run this test
        // - get an working Aurora or working Power Apps environment
        // - create a canva, add "PowerApps for Makers" connector and run expression below (GetConnectors)
        // - set GetEnvironment(), GetEndpoint(), GetConnectionId(), GetJWTToken() values in this class
        // - run the test
        // NOTE: This test is very slow (takes ~15 minutes)
        [Fact(Skip="Only for internal use")]
        public async Task PowerAppsForMakers_GetConnectors()
        {
            string swaggerRoot = @"C:\Temp\Swaggers";

            // Evaluating expressions with swagger compatibilty shouldn't be done as this might interfere with parameter ordering
            // This has no consequence for these particular connector calls
            (LoggingTestServer testConnector, OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) = GetElements(true, true);
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));
            RecalcEngine engine = new RecalcEngine(config);

            // Get list of connectors
            string expr = $"PowerAppsForMakers.GetConnectors({{ showApisWithToS: true, '$filter': \"environment eq '{GetEnvironment()}'\"}})";
            FormulaValue fv = await engine.EvalAsync(expr, CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            if (fv is ErrorValue ev)
            {
                Assert.True(false, $"{string.Join(", ", ev.Errors.Select(er => er.Message))}");
            }

            // Extract all connector names from result (key: name, value: display name)
            Dictionary<string, string> connectorNames = new Dictionary<string, string>();
            RecordValue rv = (RecordValue)fv;
            TableValue tv = (TableValue)rv.GetField("value");
            foreach (DValue<RecordValue> drv in tv.Rows)
            {
                connectorNames.Add(((StringValue)drv.Value.GetField("name")).Value, ((StringValue)((RecordValue)drv.Value.GetField("properties")).GetField("displayName")).Value);
            }

            EmptyFolder(swaggerRoot);

            // Retrieve swagger definition file for each connector
            foreach (KeyValuePair<string, string> connectorName in connectorNames)
            {
                bool tryAgain = true;
                int retryCount = 0;

                while (tryAgain && retryCount < 10)
                {
                    _output.WriteLine($"Processing {connectorName.Key}... (#{retryCount})");
                    tryAgain = false;

                    // Get connector properties (containing swagger file definition)
                    // As we use ConnectorSettings with ReturnUnknownRecordFieldsAsUntypedObjects we'll get get a FormulaValue with 'swagger' field in "properties"
                    // We can't use '.properties" in the expression as the IR isn't containing 'swagger' field (as it's not described in the PowerAppsForMakers swagger definition) and it would be removed in RecordValue.GetFieldAsync
                    expr = $"PowerAppsForMakers.GetConnector(\"{connectorName.Key}\", {{'$filter': \"environment eq '{GetEnvironment()}'\"}})";
                    fv = await engine.EvalAsync(expr, CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: runtimeConfig).ConfigureAwait(false);

                    if (fv is ErrorValue ev2)
                    {
                        string errors = string.Join("|", ev2.Errors.Select(er => er.Message));
                        _output.WriteLine($"ERROR: {errors}");

                        // HTTP error 429 management
                        Match m = new Regex("Rate limit is exceeded. Try again in (?<d>[0-9]+) seconds").Match(errors);
                        if (m.Success)
                        {
                            double duration = double.Parse(m.Groups["d"].Value);
                            long ticks = (long)(((duration * 1.05) + 2.0) * 10000000.0);
                            Thread.Sleep(new TimeSpan(ticks));

                            tryAgain = true;
                            retryCount++;
                        }
                    }
                    else
                    {
                        rv = (RecordValue)fv;
                        UntypedObjectValue uo = (UntypedObjectValue)((RecordValue)rv.GetField("properties")).GetField("swagger");

                        // Here we assume UO implementation is JsonUntypedObject
                        string swagger = ((JsonUntypedObject)uo.Impl)._element.ToString();

                        File.WriteAllText($@"{swaggerRoot}\{connectorName.Value.Replace("/", "_")}.json", IndentJson(swagger));
                        _output.WriteLine("OK");
                    }
                }
            }
        }

        // Format Json with indentation
        public static string IndentJson(string str) => JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(str), new JsonSerializerOptions() { WriteIndented = true });
    }
}
