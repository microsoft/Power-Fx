// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Tests;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class InternalTesting
    {
        // This test is only meant for internal testing
        [Fact(Skip = "Need files from AAPT-connector and PowerPlatformConnectors projects")]
        public void TestAllConnectors()
        {
            int i = 0;
            int j = 0;

            string outFolder = @"c:\temp\out";
            string srcFolder = @"c:\data";

            // Store results for analysis
            using StreamWriter writer = new StreamWriter(outFolder, append: false);

            foreach (string swaggerFile in Directory.EnumerateFiles(@$"{srcFolder}\AAPT-connectors\src", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })
                                    .Union(Directory.EnumerateFiles(@$"{srcFolder}\PowerPlatformConnectors", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })))
            {
                i++;
                try
                {
                    OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile);

                    // Check we can get the functions
                    IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(doc);

                    var config = new PowerFxConfig();
                    using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token")
                    {
                        SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
                    };

                    // Check we can add the service (more comprehensive test)
                    config.AddService("Connector", doc, client);

                    writer.WriteLine($"{swaggerFile}: OK - functions: {functions.Count()}");
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"{swaggerFile}: Exception {ex.GetType().Name} - {ex.Message}");
                    j++;
                }
            }

            writer.WriteLine($"Total: {i} - Exceptions: {j}");
        }
    }
}
