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
            using StreamWriter writer = new StreamWriter(Path.Combine(outFolder, "Analysis.txt"), append: false);

            Dictionary<string, int> exceptionMessages = new ();
            Dictionary<string, IEnumerable<ConnectorFunction>> allFunctions = new ();

            foreach (string swaggerFile in Directory.EnumerateFiles(@$"{srcFolder}\AAPT-connectors\src", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })
                                    .Union(Directory.EnumerateFiles(@$"{srcFolder}\PowerPlatformConnectors", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })))
            {
                string title = $"<Unknown Name> [{swaggerFile}]";
                i++;
                try
                {
                    OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile);
                    title = $"{doc.Info.Title} [{swaggerFile}]";

                    // Check we can get the functions
                    IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(doc);
                    Assert.True(functions.Count() > 0, "No function found");

                    allFunctions.Add(title, functions);
                    var config = new PowerFxConfig();
                    using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token")
                    {
                        SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
                    };

                    // Check we can add the service (more comprehensive test)
                    config.AddService("Connector", doc, client);                    
                }
                catch (Exception ex)
                {
                    string key = $"{ex.GetType().Name}: {ex.Message}".Split("\r\n")[0];

                    writer.WriteLine($"{title}: Exception {key}");
                    j++;                    

                    if (exceptionMessages.ContainsKey(key))
                    {
                        exceptionMessages[key]++;
                    }
                    else
                    {
                        exceptionMessages.Add(key, 1);
                    }
                }
            }

            writer.WriteLine();
            writer.WriteLine("----------");

            foreach (var kvp in allFunctions.OrderBy(k => k.Key))
            {
                var title = kvp.Key;
                var functions = kvp.Value;

                int notSupportedFunctionCount = functions.Count(f => !f.IsSupported);
                if (notSupportedFunctionCount == 0)
                {
                    writer.WriteLine($"{title}: OK - All {functions.Count()} functions are supported - [{string.Join(", ", functions.Select(f => f.Name))}]");
                }
                else
                {
                    int fCount = functions.Count();
                    if (notSupportedFunctionCount == fCount)
                    {
                        writer.WriteLine($"{title}: None of the {notSupportedFunctionCount} functions is supported, reasons: {string.Join(", ", functions.Select(f => $"{f.Name}: '{f.NotSupportedReason}'"))}");
                    }
                    else
                    {
                        writer.WriteLine($"{title}: OK - {fCount - notSupportedFunctionCount} supported functions [{string.Join(", ", functions.Where(f => f.IsSupported).Select(f => f.Name))}], " +
                                         $"{notSupportedFunctionCount} not supported functions, reasons: {string.Join(", ", functions.Where(f => !f.IsSupported).Select(f => $"{f.Name}: '{f.NotSupportedReason}'"))}");
                    }
                }
            }

            writer.WriteLine();
            writer.WriteLine("----------");
            writer.WriteLine($"Total: {i} - Exceptions: {j}");
            writer.WriteLine();

            foreach (KeyValuePair<string, int> kvp in exceptionMessages.OrderByDescending(kvp => kvp.Value))
            {
                writer.WriteLine("[{0,4} times] {1}", kvp.Value, kvp.Key);
            }

            writer.WriteLine();
            writer.WriteLine("----------");
            writer.WriteLine();

            Dictionary<string, int> notSupportedReasons = new ();
            foreach (var unsupportedFunction in allFunctions.SelectMany(kvp => kvp.Value.Where(f => !f.IsSupported).Select(f => new { SwaggerFile = kvp.Key, Function = f })))
            {
                string nsr = unsupportedFunction.Function.NotSupportedReason;
                if (notSupportedReasons.ContainsKey(nsr))
                {
                    notSupportedReasons[nsr]++;
                }
                else
                {
                    notSupportedReasons.Add(nsr, 1);
                }
            }

            foreach (KeyValuePair<string, int> kvp in notSupportedReasons.OrderByDescending(kvp => kvp.Value))
            {
                writer.WriteLine("[{0,4} times] {1}", kvp.Value, kvp.Key);
            }
        }

        [Fact(Skip = "Need files from AAPT-connector and PowerPlatformConnectors projects")]
        public void TestConnector1()
        {
            string swaggerFile = @"c:\data\AAPT-connectors\src\ConnectorPlatform\build-system\SharedTestAssets\Assets\BaselineBuild\locPublish\Connectors\AzureAD\apidefinition.swagger.json";
            OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile);
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions(doc);

            var config = new PowerFxConfig();
            using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token") { SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f" };
            
            config.AddService("Connector", doc, client);
        }
    }
}
