// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Tests;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.PowerFx.Connectors.OpenApiExtensions;

#if !NET462
using Microsoft.PowerFx.TexlFunctionExporter;

namespace Microsoft.PowerFx.Connectors.Tests
{
    [TestCaseOrderer("Microsoft.PowerFx.Connectors.Tests.PriorityOrderer", "Microsoft.PowerFx.Connectors.Tests")]
    public class InternalTesting
    {
        public readonly ITestOutputHelper _output;

        public InternalTesting(ITestOutputHelper output)
        {
            _output = output;
        }

        // This test is only meant for internal testing
#if GENERATE_CONNECTOR_STATS
        [Fact]
#else
        [Fact(Skip = "Need files from AAPT-connector and PowerPlatformConnectors projects")]
#endif
        public void TestAllConnectors()
        {
            (string outFolder, string srcFolder) = GetFolders();

            string reportFolder = @"report";
            string reportName = @$"{reportFolder}\Analysis.txt";
            string jsonReport = @$"{reportFolder}\Report.json";

            // New report name every second
            string jsonReport2 = @$"{reportFolder}\Report_{Math.Round(DateTime.UtcNow.Ticks / 1e7):00000000000}.json";

            string outFolderPath = Path.Combine(outFolder, reportFolder);
            BaseConnectorTest.EmptyFolder(outFolderPath);
            Directory.CreateDirectory(Path.Combine(outFolder, "report"));

            GenerateReport(reportFolder, reportName, outFolder, srcFolder);
            AnalyzeReport(reportName, outFolder, srcFolder, jsonReport);

            File.Copy(Path.Combine(outFolder, jsonReport), Path.Combine(outFolder, jsonReport2));
        }

        [Fact]
        public void DisplayEnvVariables()
        {
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(x => x.Key))
            {
                _output.WriteLine($"{envVar.Key} = {envVar.Value}");
            }
        }

        private (string outFolder, string srcFolder) GetFolders()
        {
            string outFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\..\.."));
            string srcFolder = Path.GetFullPath(Path.Combine(outFolder, ".."));

            // On build servers: ENV: C:\__w\1\s\pfx\src\tests\Microsoft.PowerFx.Connectors.Tests\bin\Release\netcoreapp3.1
            // Locally         : ENV: C:\Data\Power-Fx\src\tests\Microsoft.PowerFx.Connectors.Tests\bin\Debug\netcoreapp3.1
            _output.WriteLine($"ENV: {Environment.CurrentDirectory}");

            // On build servers: OUT: C:\__w\1\s\pfx
            // Locally         : OUT: C:\Data\Power-Fx
            _output.WriteLine($"OUT: {outFolder}");

            // On build servers: SRC: C:\__w\1\s
            // Locally         : SRC: C:\Data
            _output.WriteLine($"SRC: {srcFolder}");

            return (outFolder, srcFolder);
        }

        private void AnalyzeReport(string reportName, string outFolder, string srcFolder, string jsonReport)
        {
            List<Connector> connectors = new ();
            string[] lines = File.ReadAllLines(Path.Combine(outFolder, reportName));
            Regex rex = new Regex(@$"(.*) \[({srcFolder.Replace("\\", "\\\\")}.*)\]: (.*)", RegexOptions.Compiled);
            Regex rex2 = new Regex(@"OK - All ([0-9]+) functions are supported - \[([^\]]*)\]", RegexOptions.Compiled);
            Regex rex3 = new Regex(@"OK - ([0-9]+) supported functions \[([^\]]+)\], ([0-9]+) not supported functions(.*)", RegexOptions.Compiled);
            Regex rex4 = new Regex(@"(?<func>[^' ]*): ((?<dep>'OpenApiOperation is deprecated')|(?<uns>'[^']+'))");
            Regex rex5 = new Regex(@"None of the ([0-9]+) functions is supported, (.*)", RegexOptions.Compiled);

            int totalConnectors = 0;
            int totalRed = 0;
            int totalGreen = 0;
            int totalOrange = 0;
            Dictionary<string, long> r = new ();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("-----") || line.StartsWith("Total:"))
                {
                    continue;
                }

                // @"(.*) \[(c:\\data.*)\]: (.*)"
                Match m = rex.Match(line);

                if (m.Success)
                {
                    string connectorName = m.Groups[1].Value;
                    string swaggerFile = m.Groups[2].Value;
                    string result = m.Groups[3].Value;
                    string rawResult = result;

                    bool parseError = false;
                    bool ok = true;
                    bool loadsFine = true;
                    bool allFunctionsSupported = false;
                    bool noSupported = false;
                    int supportedFunctions = 0;
                    int unsupportedFunctions = 0;
                    int deprecatedFunctions = 0;
                    string supportedFunctionList = string.Empty;
                    string deprecatedFunctionList = string.Empty;
                    string unsupportedFunctionList = string.Empty;
                    List<(string f, string ur)> ufl = new ();

                    if (connectorName == "<Unknown Name>")
                    {
                        parseError = true;
                        supportedFunctionList = deprecatedFunctionList = unsupportedFunctionList = "<Unknown>";
                    }

                    if (result.StartsWith("Exception"))
                    {
                        ok = false;
                        loadsFine = false;
                        supportedFunctionList = deprecatedFunctionList = unsupportedFunctionList = "<Unknown>";
                    }

                    if (ok && result.StartsWith("None of the"))
                    {
                        // @"None of the ([0-9]+) functions is supported, (.*)"
                        Match m5 = rex5.Match(result);
                        supportedFunctions = 0;
                        noSupported = true;

                        // @"(?<func>[^' ]*): ((?<dep>'OpenApiOperation is deprecated')|(?<uns>'[^']+'))"
                        MatchCollection m4 = rex4.Matches(m5.Groups[2].Value);
                        IEnumerable<string> d4 = m4.Where(m => !string.IsNullOrEmpty(m.Groups["dep"].Value)).Select(m => m.Groups["func"].Value).OrderBy(x => x);
                        deprecatedFunctions = d4.Count();
                        deprecatedFunctionList = string.Join(", ", d4);

                        IEnumerable<string> u4 = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select(m => m.Groups["func"].Value).OrderBy(x => x);
                        unsupportedFunctionList = string.Join(", ", u4);
                        unsupportedFunctions = u4.Count();

                        if (int.Parse(m5.Groups[1].Value) != deprecatedFunctions + unsupportedFunctions)
                        {
                            throw new Exception("Invalid!");
                        }

                        ok = deprecatedFunctions > 0;
                        if (unsupportedFunctions == 0)
                        {
                            allFunctionsSupported = true;
                        }

                        IEnumerable<string> unr = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select(m => m.Groups["uns"].Value).Distinct().OrderBy(x => x);
                        result = unr.Any() ? $"Unsupported reasons: {string.Join(", ", unr)}" : string.Empty;

                        ufl = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select<Match, (string f, string ur)>(m => (m.Groups["func"].Value, m.Groups["uns"].Value)).ToList();
                    }

                    if (ok && result.StartsWith("OK - All"))
                    {
                        allFunctionsSupported = true;
                    }

                    if (ok && allFunctionsSupported && !noSupported)
                    {
                        // @"OK - All ([0-9]+) functions are supported - \[([^\]]*)\]"
                        Match m2 = rex2.Match(line);
                        supportedFunctions = int.Parse(m2.Groups[1].Value);
                        supportedFunctionList = string.Join(", ", m2.Groups[2].Value.Split(',').Select(x => x.Trim()).OrderBy(x => x));
                        result = string.Empty;
                    }

                    if (ok && !allFunctionsSupported && !noSupported)
                    {
                        // @"OK - ([0-9]+) supported functions \[([^\]]+)\], ([0-9]+) not supported functions(.*)"
                        Match m3 = rex3.Match(result);
                        supportedFunctions = int.Parse(m3.Groups[1].Value);
                        supportedFunctionList = string.Join(", ", m3.Groups[2].Value.Split(',').Select(x => x.Trim()).OrderBy(x => x));

                        // @"(?<func>[^' ]*): ((?<dep>'OpenApiOperation is deprecated')|(?<uns>'[^']+'))"
                        MatchCollection m4 = rex4.Matches(result);
                        IEnumerable<string> d4 = m4.Where(m => !string.IsNullOrEmpty(m.Groups["dep"].Value)).Select(m => m.Groups["func"].Value).OrderBy(x => x);
                        deprecatedFunctions = d4.Count();
                        deprecatedFunctionList = string.Join(", ", d4);

                        IEnumerable<string> u4 = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select(m => m.Groups["func"].Value).OrderBy(x => x);
                        unsupportedFunctionList = string.Join(", ", u4);
                        unsupportedFunctions = u4.Count();

                        if (int.Parse(m3.Groups[3].Value) != deprecatedFunctions + unsupportedFunctions)
                        {
                            throw new Exception("Invalid!");
                        }

                        if (unsupportedFunctions == 0)
                        {
                            allFunctionsSupported = true;
                        }

                        IEnumerable<string> unr = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select(m => m.Groups["uns"].Value).Distinct();
                        result = unr.Any() ? $"Unsupported reasons: {string.Join(", ", unr)}" : string.Empty;

                        ufl = m4.Where(m => !string.IsNullOrEmpty(m.Groups["uns"].Value)).Select<Match, (string f, string ur)>(m => (m.Groups["func"].Value, m.Groups["uns"].Value)).ToList();
                    }

                    Color connectorNameColor = !ok ? Color.Red : (allFunctionsSupported || unsupportedFunctions == 0) ? Color.Green : Color.Orange;
                    Color supportedColor = !ok ? Color.Red : Color.Green;
                    Color parseErrorColor = parseError ? Color.Red : Color.White;
                    Color loadsFineColor = loadsFine && !parseError ? Color.White : Color.Red;
                    Color allFuncSuppColor = allFunctionsSupported ? Color.Green : Color.White;
                    Color unSuppColor = ok && !allFunctionsSupported && unsupportedFunctions > 0 ? Color.Orange : Color.White;
                    Color deprecatedFuncColor = deprecatedFunctions > 0 ? Color.Orange : Color.White;

                    connectors.Add(new Connector()
                    {
                        ConnectorName = connectorName,
                        ConnectorNameColor = connectorNameColor,
                        Supported = ok,
                        SupportedColor = supportedColor,
                        ParseError = parseError,
                        ParseErrorColor = parseErrorColor,
                        Loadsfine = loadsFine,
                        LoadsFineColor = loadsFineColor,
                        AllFunctionsSupported = allFunctionsSupported,
                        AllFuncSuppColor = allFuncSuppColor,
                        SupportedFunctions = supportedFunctions,
                        SupportedFunctionList = supportedFunctionList,
                        DeprecatedFunctions = deprecatedFunctions,
                        DeprecatedFunctionList = deprecatedFunctionList,
                        DeprecatedFuncColor = deprecatedFuncColor,
                        UnsupportedFunctions = unsupportedFunctions,
                        UnsupportedFunctionList = unsupportedFunctionList,
                        UnsupportedFuncColor = unSuppColor,
                        SwaggerFile = swaggerFile,
                        Result = result,
                        RawResult = rawResult
                    });

                    totalConnectors++;
                    totalRed += connectorNameColor == Color.Red ? 1 : 0;
                    totalOrange += connectorNameColor == Color.Orange ? 1 : 0;
                    totalGreen += connectorNameColor == Color.Green ? 1 : 0;
                }
            }

            "Stats".Dump(_output);
            $"Total: {totalConnectors}".Dump(_output);
            string.Empty.Dump(_output);
            string.Format("- Supported: {0} - {1:0.00} %", totalGreen + totalOrange, (totalGreen + totalOrange) * 100m / totalConnectors).Dump(_output);
            string.Format("- Not Supported: {0} - {1:0.00} %", totalRed, totalRed * 100m / totalConnectors).Dump(_output);
            string.Empty.Dump(_output);
            $"- Red: {totalRed}".Dump(_output);
            $"- Orange: {totalOrange}".Dump(_output);
            $"- Green: {totalGreen}".Dump(_output);
            string.Empty.Dump(_output);

            List<Connector> orderedConnectors = connectors.OrderBy(c => (c.ParseError ? "0" : "1") + c.ConnectorName).ToList();
            orderedConnectors.Dump(_output);

            // JSON export
            string json = JsonSerializer.Serialize(orderedConnectors, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(Path.Combine(outFolder, jsonReport), json);
        }

        public class Connector
        {
            public string ConnectorName { get; set; }

            public string ConnectorNameColorStr => ConnectorNameColor.ToString();

            internal Color ConnectorNameColor { get; set; }

            public bool Supported { get; set; }

            public string SupportedColorStr => SupportedColor.ToString();

            internal Color SupportedColor { get; set; }

            public bool ParseError { get; set; }

            public string ParseErrorColorStr => ParseErrorColor.ToString();

            internal Color ParseErrorColor { get; set; }

            public bool Loadsfine { get; set; }

            public string LoadsFineColorStr => LoadsFineColor.ToString();

            internal Color LoadsFineColor { get; set; }

            public bool AllFunctionsSupported { get; set; }

            public string AllFuncSuppColorStr => AllFuncSuppColor.ToString();

            internal Color AllFuncSuppColor { get; set; }

            public int SupportedFunctions { get; set; }

            public string SupportedFunctionList { get; set; }

            public int DeprecatedFunctions { get; set; }

            public string DeprecatedFuncColorStr => DeprecatedFuncColor.ToString();

            public string DeprecatedFunctionList { get; set; }

            internal Color DeprecatedFuncColor { get; set; }

            public int UnsupportedFunctions { get; set; }

            public string UnsupportedFuncColorStr => UnsupportedFuncColor.ToString();

            public string UnsupportedFunctionList { get; set; }

            internal Color UnsupportedFuncColor { get; set; }

            public string SwaggerFile { get; set; }

            public string Result { get; set; }

            public string RawResult { get; set; }

            public override string ToString()
            {
                return string.Join("\t", typeof(Connector).GetProperties().Select(pi => pi.GetValue(this)));
            }
        }

        public enum Color
        {
            Red = 0,    // #FF4B4B
            Green = 1,  // #8ED973
            Orange = 2, // #FF9900
            White = 3,  // #FFFFFF
        }

        private void GenerateReport(string reportFolder, string reportName, string outFolder, string srcFolder)
        {
            int i = 0;
            int j = 0;
            using StreamWriter writer = new StreamWriter(Path.Combine(outFolder, reportName), append: false);

            Dictionary<string, List<string>> w2 = new Dictionary<string, List<string>>();
            Dictionary<string, int> exceptionMessages = new ();
            Dictionary<string, IEnumerable<ConnectorFunction>> allFunctions = new ();

            // To create aapt and ppc folders locally, you can use NTFS junctions. Ex: mklink /J ppc <folder to PowerPlatformConnectors>
            foreach (string swaggerFile in Directory.EnumerateFiles(@$"{srcFolder}\aapt\src", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })
                                    .Union(Directory.EnumerateFiles(@$"{srcFolder}\ppc", "apidefinition*swagger*json", new EnumerationOptions() { RecurseSubdirectories = true })))
            {
                string title = $"<Unknown Name> [{swaggerFile}]";
                i++;
                try
                {
                    ConsoleLogger logger = new ConsoleLogger(_output);
                    OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile, _output);
                    ConnectorSettings connectorSettings = new ConnectorSettings("Connector") { AllowUnsupportedFunctions = true, IncludeInternalFunctions = true };
                    ConnectorSettings swaggerConnectorSettings = new ConnectorSettings("Connector") { AllowUnsupportedFunctions = true, IncludeInternalFunctions = true, Compatibility = ConnectorCompatibility.SwaggerCompatibility };

                    title = $"{doc.Info.Title} [{swaggerFile}]";

                    // Check we can get the functions
                    IEnumerable<ConnectorFunction> functions1 = OpenApiParser.GetFunctions(connectorSettings, doc, logger);

                    allFunctions.Add(title, functions1);
                    var config = new PowerFxConfig();

                    // Check we can add the service (more comprehensive test)
                    config.AddActionConnector(connectorSettings, doc, logger);

                    IEnumerable<ConnectorFunction> functions2 = OpenApiParser.GetFunctions(swaggerConnectorSettings, doc);
                    string cFolder = Path.Combine(outFolder, reportFolder, doc.Info.Title);

                    int ix = 2;
                    while (Directory.Exists(cFolder))
                    {
                        cFolder = Path.Combine(outFolder, reportFolder, doc.Info.Title) + $"_{ix++}";
                    }

                    Directory.CreateDirectory(cFolder);

                    foreach (ConnectorFunction cf1 in functions1)
                    {
                        ConnectorFunction cf2 = functions2.First(f => f.Name == cf1.Name);

                        if (cf1.RequiredParameters != null && cf2.RequiredParameters != null)
                        {
                            string rp1 = string.Join(", ", cf1.RequiredParameters.Select(rp => rp.Name));
                            string rp2 = string.Join(", ", cf2.RequiredParameters.Select(rp => rp.Name));

                            if (rp1 != rp2)
                            {
                                string s = $"Function {cf1.Name} - Required parameters are different: [{rp1}] -- [{rp2}]";
                                if (w2.TryGetValue(title, out List<string> value))
                                {
                                    value.Add(s);
                                }
                                else
                                {
                                    w2.Add(title, new List<string>() { s });
                                }
                            }
                        }

                        string functionFile = Path.Combine(cFolder, cf1.OriginalName.Replace("/", "_") + ".yaml");
                        File.WriteAllText(functionFile, new YamlConnectorFunction(cf1, swaggerFile).GetYaml());
                    }
                }
                catch (Exception ex)
                {
                    string key = $"{ex.GetType().Name}: {ex.Message}".Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];

                    writer.WriteLine($"{title}: Exception {key}");
                    j++;

                    if (exceptionMessages.TryGetValue(key, out var value))
                    {
                        exceptionMessages[key] = ++value;
                    }
                    else
                    {
                        exceptionMessages.Add(key, 1);
                    }
                }

                using StreamWriter writer2 = new StreamWriter(Path.Combine(outFolder, reportFolder, "ConnectorComparison.txt"), append: false);

                foreach (KeyValuePair<string, List<string>> kvp in w2.OrderBy(kvp => kvp.Key))
                {
                    writer2.WriteLine($"-- {kvp.Key} --");

                    foreach (string s in kvp.Value.OrderBy(x => x))
                    {
                        writer2.WriteLine(s);
                    }

                    writer2.WriteLine();
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
                if (notSupportedReasons.TryGetValue(nsr, out var value))
                {
                    notSupportedReasons[nsr] = ++value;
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
            OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile, _output);
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("C", doc, new ConsoleLogger(_output));

            var config = new PowerFxConfig();
            using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token") { SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f" };

            config.AddActionConnector("Connector", doc, new ConsoleLogger(_output));
        }

        /// <summary>
        /// GenerateYamlFiles from Connector TexlFunctions.
        /// </summary>
        /// <param name="reference">Name of the test.</param>
        /// <param name="folderExclusionIndex">0: default folder exclusions, 1: no exclusion.</param>
        /// <param name="pattern">Pattern to identify swagger files.</param>
        /// <param name="folders">List of folders to consider when identifying swagger files. 
        /// Wehn no folder is provided, use internal Library.
        /// When 2 swagger files will have the same display name and version, the one from first folder will be preferred.
        /// </param>        
        // This test is only meant for internal testing
#if GENERATE_CONNECTOR_STATS
        [Theory]        
#else
        [Theory(Skip = "Need files from AAPT-connector, PowerPlatformConnectors and Power-Fx-TexlFunctions-Baseline projects")]
#endif
        [TestPriority(1)]
        [InlineData("Library")] // Default Power-Fx library
        [InlineData("Aapt-Ppc", 0, "apidefinition*swagger*.json", @"aapt\src\Connectors", @"ppc")]
        [InlineData("Baseline", 1, "*.json", @"Power-Fx-TexlFunctions-Baseline\Swaggers")]
        public void GenerateYamlFiles(string reference, int folderExclusionIndex = -1, string pattern = null, params string[] folders)
        {
            (string outFolder, string srcFolder) = GetFolders();

            string outFolderPath = Path.Combine(outFolder, "YamlOutput");
            BaseConnectorTest.EmptyFolder(Path.Combine(outFolderPath, reference));

            // if no folder, use Library
            if (folders.Length == 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                List<TexlFunction> texlFunctions = BuiltinFunctionsCore._library.Functions.ToList();
#pragma warning restore CS0618 // Type or member is obsolete

                _output.WriteLine($"Number of TexlFunctions in library: {texlFunctions.Count()}");

                // Export to Yaml
                ExportTexlFunctionsToYaml(reference, outFolderPath, reference, texlFunctions, true);
                return;
            }

            SwaggerLocatorSettings swaggerLocationSettings = folderExclusionIndex == 0 ? null : new SwaggerLocatorSettings(new List<string>());
            ConsoleLogger logger = new ConsoleLogger(_output);
            ConnectorSettings connectorSettings = new ConnectorSettings("NS")
            {
                AllowUnsupportedFunctions = true,
                IncludeInternalFunctions = true,
                FailOnUnknownExtension = false,
                Compatibility = ConnectorCompatibility.PowerAppsCompatibility
            };

            string[] rootedFolders = folders.Select(f => Path.Combine(srcFolder, f)).ToArray();

            // Step 1: Identify the list of swagger files to consider
            // The output of LocateSwaggerFilesWithDocuments is a dictionary where
            // - Key is the connector display name
            // - Value is a (folder, location, document) tuple where folder is the source folder at the origin of the swagger identification, location is the exact swagger file location, document is the corresponding OpenApiDocument 
            // LocateSwaggerFiles could also be used here and would only return a Dictionary<displayName, location> (no source folder or document)
            Dictionary<string, (string folder, string location, OpenApiDocument document, List<string> errors)> swaggerFiles = SwaggerFileIdentification.LocateSwaggerFilesWithDocuments(rootedFolders, pattern, swaggerLocationSettings);
            _output.WriteLine($"Total number of connectors found: {swaggerFiles.Count()}");
            _output.WriteLine($"Number of connectors found: {swaggerFiles.Count(sf => !sf.Key.StartsWith(SwaggerFileIdentification.UNKNOWN_SWAGGER))}");

            foreach (KeyValuePair<string, (string folder, string location, OpenApiDocument document, List<string> errors)> connector in swaggerFiles)
            {
                if (connector.Value.errors != null && connector.Value.errors.Any())
                {
                    // Log OpenApi errors
                    string folderName = $"{Path.Combine(outFolderPath, reference, connector.Key.Replace("/", "_", StringComparison.OrdinalIgnoreCase))}";
                    Directory.CreateDirectory(folderName);
                    File.WriteAllText(Path.Combine(folderName, "OpenApiErrors.txt"), string.Join("\r\n", new string[] { connector.Value.location }.Union(connector.Value.errors)));
                }

                if (connector.Key.StartsWith(SwaggerFileIdentification.UNKNOWN_SWAGGER))
                {
                    continue;
                }

                // Step 2: Get TexlFunctions to be exported
                // Notice that TexlFunction is internal and requires InternalVisibleTo
                (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) = OpenApiParser.ParseInternal(connectorSettings, connector.Value.document, logger);

                // Step 3: Export TexlFunctions to Yaml
                ExportTexlFunctionsToYaml(reference, outFolderPath, connector.Key, texlFunctions.Cast<TexlFunction>().ToList(), false);

                // Step 3: Export TexlFunctions to Yaml
                ExportConnectorFunctionsToYaml(reference, outFolderPath, connector.Key, connectorFunctions);
            }
        }

#if GENERATE_CONNECTOR_STATS
        [Fact]        
#else
        [Fact(Skip = "Need files from AAPT-connector, PowerPlatformConnectors and Power-Fx-TexlFunctions-Baseline projects")]
#endif

        // Executes after GenerateYamlFiles
        [TestPriority(2)]
        public void YamlCompare()
        {
            (string outFolder, string srcFolder) = GetFolders();

            string yamlFiles = Path.Combine(outFolder, "YamlOutput");
            string yamlReference = Path.Combine(srcFolder, @"Power-Fx-TexlFunctions-Baseline\Yaml");

            List<ConnectorStat> connectorStats = new List<ConnectorStat>();
            List<FunctionStat> functionStats = new List<FunctionStat>();

            // Compare Texl functions with baseline
            new TexlYamlComparer("BaseLine", yamlReference, yamlFiles, connectorStats, functionStats, msg => _output.WriteLine(msg)).CompareYamlFiles();
            new TexlYamlComparer("Aapt-Ppc", yamlReference, yamlFiles, connectorStats, functionStats, msg => _output.WriteLine(msg)).CompareYamlFiles();
            new TexlYamlComparer("Library", yamlReference, yamlFiles, connectorStats, functionStats, msg => _output.WriteLine(msg)).CompareYamlFiles();

            // Compare ConnectorFunction with baseline
            new ConnectorFunctionYamlComparer("BaseLine", yamlReference, yamlFiles, connectorStats, functionStats, msg => _output.WriteLine(msg)).CompareYamlFiles();
            new ConnectorFunctionYamlComparer("Aapt-Ppc", yamlReference, yamlFiles, connectorStats, functionStats, msg => _output.WriteLine(msg)).CompareYamlFiles();

            // Display results
            _output.WriteLine(string.Empty);
            _output.WriteLine("Baseline (Texl)");
            foreach (ConnectorStat connectorStat in connectorStats.Where(cs => cs.Category == "BaseLine_Texl"))
            {
                _output.WriteLine(connectorStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("Baseline (Texl) - functions");
            foreach (FunctionStat functionStat in functionStats.Where(cs => cs.Category == "BaseLine_Texl"))
            {
                _output.WriteLine(functionStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Baseline (ConnectorFunction) --");
            foreach (ConnectorStat connectorStat in connectorStats.Where(cs => cs.Category == "BaseLine_ConnectorFunction"))
            {
                _output.WriteLine(connectorStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Baseline (ConnectorFunction) - functions --");
            foreach (FunctionStat functionStat in functionStats.Where(cs => cs.Category == "BaseLine_ConnectorFunction"))
            {
                _output.WriteLine(functionStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Aapt-Ppc (Texl) --");
            foreach (ConnectorStat connectorStat in connectorStats.Where(cs => cs.Category == "Aapt-Ppc_Texl"))
            {
                _output.WriteLine(connectorStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("--Aapt-Ppc (Texl) - functions --");
            foreach (FunctionStat functionStat in functionStats.Where(cs => cs.Category == "Aapt-Ppc_Texl"))
            {
                _output.WriteLine(functionStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Aapt-Ppc (ConnectorFunction) --");
            foreach (ConnectorStat connectorStat in connectorStats.Where(cs => cs.Category == "Aapt-Ppc_ConnectorFunction"))
            {
                _output.WriteLine(connectorStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Aapt-Ppc (ConnectorFunction) - functions --");
            foreach (FunctionStat functionStat in functionStats.Where(cs => cs.Category == "Aapt-Ppc_ConnectorFunction"))
            {
                _output.WriteLine(functionStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Library (Texl) --");
            foreach (ConnectorStat connectorStat in connectorStats.Where(cs => cs.Category == "Library_Texl"))
            {
                _output.WriteLine(connectorStat.ToString());
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine("-- Library (Texl) - functions --");
            foreach (FunctionStat functionStat in functionStats.Where(cs => cs.Category == "Library_Texl"))
            {
                _output.WriteLine(functionStat.ToString());
            }

            // Upload to SQL 
            string connectionString = Environment.GetEnvironmentVariable("PFXDEV_CONNECTORANALYSIS");
            string buildId = Environment.GetEnvironmentVariable("BUILD_ID"); // int
            string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER"); // string            

            SqlConnectionStringBuilder csb = new (connectionString)
            {
                CommandTimeout = 300,
                ConnectTimeout = 30
            };

            using SqlConnection connection = new SqlConnection(csb.ConnectionString);

            connection.Open();

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = "dbo.Connectors" })
            {
                using DataTable connectorsTable = GetConnectorsTable();

                foreach (ConnectorStat connectorStat in connectorStats)
                {
                    DataRow row = connectorsTable.NewRow();

                    row[0] = buildId;
                    row[1] = buildNumber;
                    row[2] = connectorStat.Category;
                    row[3] = connectorStat.ConnectorName;
                    row[4] = connectorStat.Functions;
                    row[5] = connectorStat.Supported ?? (object)DBNull.Value;
                    row[6] = connectorStat.WithWarnings ?? (object)DBNull.Value;
                    row[7] = connectorStat.Deprecated ?? (object)DBNull.Value;
                    row[8] = connectorStat.Internal ?? (object)DBNull.Value;
                    row[9] = connectorStat.Pageable ?? (object)DBNull.Value;
                    row[10] = connectorStat.OpenApiErrors ?? (object)DBNull.Value;
                    row[11] = connectorStat.DifferFromBaseline;
                    row[12] = connectorStat.Differences == null ? (object)DBNull.Value : string.Join(", ", connectorStat.Differences);

                    connectorsTable.Rows.Add(row);
                }

                bulkCopy.BatchSize = 1000;
                bulkCopy.BulkCopyTimeout = 0;
                bulkCopy.WriteToServer(connectorsTable);

                _output.WriteLine(string.Empty);
                _output.WriteLine($"Copied {bulkCopy.RowsCopied} rows in Connectors table");
            }

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = "dbo.Functions" })
            {
                using DataTable functionsTable = GetFunctionsTable();

                foreach (FunctionStat functionStat in functionStats)
                {
                    DataRow row = functionsTable.NewRow();

                    row[0] = buildId;
                    row[1] = buildNumber;
                    row[2] = functionStat.Category;
                    row[3] = functionStat.ConnectorName;
                    row[4] = functionStat.FunctionName;
                    row[5] = functionStat.IsSupported ?? (object)DBNull.Value;
                    row[6] = functionStat.NotSupportedReason ?? (object)DBNull.Value;
                    row[7] = functionStat.Warnings ?? (object)DBNull.Value;
                    row[8] = functionStat.IsDeprecated ?? (object)DBNull.Value;
                    row[9] = functionStat.IsInternal ?? (object)DBNull.Value;
                    row[10] = functionStat.IsPageable ?? (object)DBNull.Value;
                    row[11] = functionStat.ArityMin;
                    row[12] = functionStat.ArityMax;
                    row[13] = functionStat.RequiredParameterTypes ?? (object)DBNull.Value;
                    row[14] = functionStat.OptionalParameterTypes ?? (object)DBNull.Value;
                    row[15] = functionStat.ReturnType ?? (object)DBNull.Value;
                    row[16] = functionStat.Parameters ?? (object)DBNull.Value;
                    row[17] = functionStat.DifferFromBaseline;
                    row[18] = functionStat.Differences == null ? (object)DBNull.Value : string.Join(", ", functionStat.Differences);
                    row[19] = functionStat.RequiredParameterSchemas ?? (object)DBNull.Value;
                    row[20] = functionStat.OptionalParameterSchemas ?? (object)DBNull.Value;
                    row[21] = functionStat.ReturnSchema ?? (object)DBNull.Value;

                    functionsTable.Rows.Add(row);
                }

                bulkCopy.BatchSize = 1000;
                bulkCopy.BulkCopyTimeout = 0;
                bulkCopy.WriteToServer(functionsTable);

                _output.WriteLine($"Copied {bulkCopy.RowsCopied} rows in Functions table");
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed outside")]
        private DataTable GetConnectorsTable()
        {
            DataTable connectors = new DataTable("Connectors");

            DataColumn buildId = new DataColumn("BuildId", typeof(int));
            DataColumn buildNumber = new DataColumn("BuildNumber", typeof(string));
            DataColumn category = new DataColumn("Category", typeof(string));
            DataColumn connectorName = new DataColumn("ConnectorName", typeof(string));
            DataColumn functions = new DataColumn("Functions", typeof(int));
            DataColumn supported = new DataColumn("Supported", typeof(int));
            DataColumn withWarnings = new DataColumn("WithWarnings", typeof(int));
            DataColumn deprecated = new DataColumn("Deprecated", typeof(int));
            DataColumn @internal = new DataColumn("Internal", typeof(int));
            DataColumn pageable = new DataColumn("Pageable", typeof(int));
            DataColumn openApiErrors = new DataColumn("OpenApiErrors", typeof(string));
            DataColumn differFromBaseline = new DataColumn("DifferFromBaseline", typeof(bool));
            DataColumn differences = new DataColumn("Differences", typeof(string));

            connectors.Columns.Add(buildId);
            connectors.Columns.Add(buildNumber);
            connectors.Columns.Add(category);
            connectors.Columns.Add(connectorName);
            connectors.Columns.Add(functions);
            connectors.Columns.Add(supported);
            connectors.Columns.Add(withWarnings);
            connectors.Columns.Add(deprecated);
            connectors.Columns.Add(@internal);
            connectors.Columns.Add(pageable);
            connectors.Columns.Add(openApiErrors);
            connectors.Columns.Add(differFromBaseline);
            connectors.Columns.Add(differences);

            return connectors;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed outside")]
        private DataTable GetFunctionsTable()
        {
            DataTable functions = new DataTable("Functions");

            DataColumn buildId = new DataColumn("BuildId", typeof(int));
            DataColumn buildNumber = new DataColumn("BuildNumber", typeof(string));
            DataColumn category = new DataColumn("Category", typeof(string));
            DataColumn connectorName = new DataColumn("ConnectorName", typeof(string));
            DataColumn functionName = new DataColumn("FunctionName", typeof(string));
            DataColumn isSupported = new DataColumn("IsSupported", typeof(bool));
            DataColumn notSupportedReason = new DataColumn("NotSupportedReason", typeof(string));
            DataColumn warnings = new DataColumn("Warnings", typeof(string));
            DataColumn isDeprecated = new DataColumn("IsDeprecated", typeof(bool));
            DataColumn isInternal = new DataColumn("IsInternal", typeof(bool));
            DataColumn isPageable = new DataColumn("IsPageable", typeof(bool));
            DataColumn arityMin = new DataColumn("ArityMin", typeof(int));
            DataColumn arityMax = new DataColumn("ArityMax", typeof(int));
            DataColumn requiredParameterTypes = new DataColumn("RequiredParameterTypes", typeof(string));
            DataColumn optionalParameterTypes = new DataColumn("OptionalParameterTypes", typeof(string));
            DataColumn returnType = new DataColumn("ReturnType", typeof(string));
            DataColumn parameters = new DataColumn("Parameters", typeof(string));
            DataColumn differFromBaseline = new DataColumn("DifferFromBaseline", typeof(bool));
            DataColumn differences = new DataColumn("Differences", typeof(string));
            DataColumn requiredParameterSchemas = new DataColumn("RequiredParameterSchemas", typeof(string));
            DataColumn optionalParameterSchemas = new DataColumn("OptionalParameterSchemas", typeof(string));
            DataColumn returnSchema = new DataColumn("ReturnSchema", typeof(string));

            functions.Columns.Add(buildId);
            functions.Columns.Add(buildNumber);
            functions.Columns.Add(category);
            functions.Columns.Add(connectorName);
            functions.Columns.Add(functionName);
            functions.Columns.Add(isSupported);
            functions.Columns.Add(notSupportedReason);
            functions.Columns.Add(warnings);
            functions.Columns.Add(isDeprecated);
            functions.Columns.Add(isInternal);
            functions.Columns.Add(isPageable);
            functions.Columns.Add(arityMin);
            functions.Columns.Add(arityMax);
            functions.Columns.Add(requiredParameterTypes);
            functions.Columns.Add(optionalParameterTypes);
            functions.Columns.Add(returnType);
            functions.Columns.Add(parameters);
            functions.Columns.Add(differFromBaseline);
            functions.Columns.Add(differences);
            functions.Columns.Add(requiredParameterSchemas);
            functions.Columns.Add(optionalParameterSchemas);
            functions.Columns.Add(returnSchema);

            return functions;
        }

        public class ConnectorFunctionYamlComparer : YamlComparer<YamlConnectorFunction>
        {
            internal override string FilePattern => "ConnectorFunction_*.yaml";

            internal override string CategorySuffix => "ConnectorFunction";

            public ConnectorFunctionYamlComparer(string category, string referenceRoot, string currentRoot, List<ConnectorStat> connectorStats, List<FunctionStat> functionStats, Action<string> log)
                : base(category, referenceRoot, currentRoot, connectorStats, functionStats, log)
            {
            }
        }

        [Theory]
        [InlineData("", "", "", "", "")]
        [InlineData("a", "", "a", "", "")]
        [InlineData("", "a", "", "", "a")]
        [InlineData("a", "a", "", "a", "")]
        [InlineData("a,b", "", "a,b", "", "")]
        [InlineData("", "a,b", "", "", "a,b")]
        [InlineData("a,b", "a,b", "", "a,b", "")]
        [InlineData("a,b", "b,c", "a", "b", "c")]
        [InlineData("b,c", "a,b", "c", "b", "a")]
        [InlineData("a", "b", "a", "", "b")]
        [InlineData("a,b", "c,d,e", "a,b", "", "c,d,e")]
        [InlineData("c,d,e", "a,b", "c,d,e", "", "a,b")]
        [InlineData("b", "a,c", "b", "", "a,c")]
        [InlineData("a,c", "b", "a,c", "", "b")]
        [InlineData("a,b,c,d,e", "b,d,f", "a,c,e", "b,d", "f")]
        public void CompareLists_Tests(string left, string right, string leftOnly, string common, string rightOnly)
        {
            (List<string> lo, List<string> c, List<string> ro) = YamlComparer.CompareLists(left.Split(",", StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x), right.Split(",", StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x), x => x);

            Assert.Equal(leftOnly.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(), lo);
            Assert.Equal(common.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(), c);
            Assert.Equal(rightOnly.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(), ro);
        }

        private static void ExportTexlFunctionsToYaml(string reference, string output, string connectorName, List<TexlFunction> texlFunctions, bool isLibrary)
        {
            foreach (TexlFunction texlFunction in texlFunctions)
            {
                // Export TexlFunction definition as Yaml file
                YamlExporter.ExportTexlFunction($"{Path.Combine(output, reference, connectorName.Replace("/", "_", StringComparison.OrdinalIgnoreCase))}", texlFunction, isLibrary);
            }
        }

        private static void ExportConnectorFunctionsToYaml(string reference, string output, string connectorName, List<ConnectorFunction> connectorFunctions)
        {
            foreach (ConnectorFunction connectorFunction in connectorFunctions)
            {
                // Export TexlFunction definition as Yaml file
                ExportConnectorFunction($"{Path.Combine(output, reference, connectorName.Replace("/", "_", StringComparison.OrdinalIgnoreCase))}", connectorFunction);
            }
        }

        private static void ExportConnectorFunction(string folder, ConnectorFunction connectorFunction)
        {
            YamlConnectorFunction function = new YamlConnectorFunction(connectorFunction, null);

            string functionFile = Path.Combine(folder, "ConnectorFunction_" + connectorFunction.Name.Replace("/", "_", StringComparison.OrdinalIgnoreCase) + ".yaml");
            Directory.CreateDirectory(folder);

            if (File.Exists(functionFile))
            {
                throw new IOException($"File {functionFile} already exists!");
            }

            File.WriteAllText(functionFile, function.GetYaml(), Encoding.UTF8);
        }
    }

    public sealed class YamlConnectorFunction : YamlReaderWriter, IYamlFunction
    {
        public YamlConnectorFunction()
        {
        }

        public YamlConnectorFunction(ConnectorFunction connectorFunction, string swaggerFile)
        {
            Name = connectorFunction.Name;
            OperationId = connectorFunction.OriginalName;
            Method = connectorFunction.HttpMethod.ToString().ToUpperInvariant();
            Path = connectorFunction.OperationPath;
            SwaggerFile = swaggerFile;
            Description = connectorFunction.Description;
            Summary = connectorFunction.Summary;
            IsBehavior = connectorFunction.IsBehavior;
            IsSupported = connectorFunction.IsSupported;
            NotSupportedReason = connectorFunction.NotSupportedReason;
            Warnings = connectorFunction.Warnings.Count > 0 ? string.Join(", ", connectorFunction.Warnings.Select(erk => ErrorUtils.FormatMessage(StringResources.Get(erk), null, Name, connectorFunction.Namespace))) : null;
            IsDeprecated = connectorFunction.IsDeprecated;
            IsInternal = connectorFunction.IsInternal;
            IsPageable = connectorFunction.IsPageable;
            RequiresUserConfirmation = connectorFunction.RequiresUserConfirmation;
            Visibility = connectorFunction.Visibility;
            ReturnType = connectorFunction.ReturnType._type.ToString();
            ReturnType_Detailed = connectorFunction.ReturnParameterType == null ? null : new YamlConnectorType(connectorFunction.ReturnParameterType);
            ArityMin = connectorFunction.ArityMin;
            ArityMax = connectorFunction.ArityMax;
            RequiredParameters = connectorFunction.RequiredParameters?.Select(rp => new YamlConnectorParameter(rp)).ToArray();
            OptionalParameters = connectorFunction.OptionalParameters?.Select(op => new YamlConnectorParameter(op)).ToArray();
        }

        public string Name;
        public string OperationId;
        public string Method;
        public string Path;
        public string SwaggerFile;
        public string Description;
        public string Summary;
        public bool IsBehavior;
        public bool IsSupported;
        public string NotSupportedReason;
        public string Warnings;
        public bool IsDeprecated;
        public bool IsInternal;
        public bool IsPageable;
        public bool RequiresUserConfirmation;
        public string Visibility;
        public string ReturnType;
        public YamlConnectorType ReturnType_Detailed;
        public int ArityMin;
        public int ArityMax;
        public YamlConnectorParameter[] RequiredParameters;
        public YamlConnectorParameter[] OptionalParameters;

        string IYamlFunction.GetName()
        {
            return Name;
        }

        bool IYamlFunction.HasDetailedProperties()
        {
            return true;
        }

        bool IYamlFunction.GetIsSupported()
        {
            return IsSupported;
        }

        bool IYamlFunction.GetIsDeprecated()
        {
            return IsDeprecated;
        }

        bool IYamlFunction.GetIsInternal()
        {
            return IsInternal;
        }

        bool IYamlFunction.GetIsPageable()
        {
            return IsPageable;
        }

        string IYamlFunction.GetNotSupportedReason()
        {
            return NotSupportedReason;
        }

        int IYamlFunction.GetArityMin()
        {
            return ArityMin;
        }

        int IYamlFunction.GetArityMax()
        {
            return ArityMax;
        }

        string IYamlFunction.GetRequiredParameterTypes()
        {
            if (RequiredParameters == null || RequiredParameters.Length == 0)
            {
                return null;
            }

            return string.Join(", ", RequiredParameters.Select(cp => cp.Type.FormulaType));
        }

        string IYamlFunction.GetOptionalParameterTypes()
        {
            if (OptionalParameters == null || OptionalParameters.Length == 0)
            {
                return null;
            }

            return string.Join(", ", OptionalParameters.Select(cp => cp.Type.FormulaType));
        }

        string IYamlFunction.GetReturnType()
        {
            return ReturnType;
        }

        string IYamlFunction.GetParameterNames()
        {
            string paramNames = string.Join(", ", RequiredParameters?.Select(rp => rp.Name) ?? Enumerable.Empty<string>());

            if (OptionalParameters?.Length > 0)
            {
                if (!string.IsNullOrEmpty(paramNames))
                {
                    paramNames += ", ";
                }

                paramNames += $"{{ {string.Join(", ", OptionalParameters.Select(op => op.Name))} }}";
            }

            return paramNames;
        }

        string IYamlFunction.GetWarnings()
        {
            return Warnings;
        }

        string IYamlFunction.GetOptionalParameterSchemas()
        {
            return OptionalParameters == null ? null : string.Join("|", OptionalParameters.Select(op => op.Type.Schema));
        }

        string IYamlFunction.GetRequiredParameterSchemas()
        {
            return RequiredParameters == null ? null : string.Join("|", RequiredParameters.Select(op => op.Type.Schema));
        }

        string IYamlFunction.GetReturnSchema()
        {
            return ReturnType_Detailed.Schema;
        }
    }

    public class YamlConnectorParameter
    {
        public YamlConnectorParameter()
        {
        }

        public YamlConnectorParameter(ConnectorParameter connectorParam)
        {
            Name = connectorParam.Name;
            Description = connectorParam.Description;
            Location = connectorParam.Location.ToString();
            FormulaType = connectorParam.FormulaType._type.ToString();
            Type = new YamlConnectorType(connectorParam.ConnectorType);

            if (connectorParam.DefaultValue != null)
            {
                StringBuilder sb = new StringBuilder();
                connectorParam.DefaultValue.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
                DefaultValue = sb.ToString();
            }

            Title = connectorParam.Title;
            ExplicitInput = connectorParam.ConnectorExtensions.ExplicitInput;
        }

        public string Name;
        public string Description;
        public string Location;
        public string FormulaType;
        public YamlConnectorType Type;
        public string DefaultValue;
        public string Title;
        public bool ExplicitInput;
    }

    public class YamlConnectorType
    {
        public YamlConnectorType()
        {
        }

        public YamlConnectorType(ConnectorType connectorType, bool noname = false)
        {
            if (!noname)
            {
                Name = connectorType.Name;
                DisplayName = connectorType.DisplayName;
                Description = connectorType.Description;
            }

            IsRequired = connectorType.IsRequired;

            if (connectorType.Fields != null && connectorType.Fields.Any())
            {
                Fields = connectorType.Fields.Select(fieldCT => new YamlConnectorType(fieldCT)).ToArray();
            }

            FormulaType = connectorType.FormulaType._type.ToString();
            ExplicitInput = connectorType.ExplicitInput;
            IsEnum = connectorType.IsEnum;

            if (connectorType.IsEnum)
            {
                bool hasDisplayNames = connectorType.EnumDisplayNames != null && connectorType.EnumDisplayNames.Length > 0;
                EnumValues = connectorType.EnumValues.Select((ev, i) => new YamlEnumValue()
                {
                    DisplayName = hasDisplayNames ? connectorType.EnumDisplayNames[i] : null,
                    Value = ev.ToObject().ToString(),
                    Type = ev.Type._type.ToString()
                }).ToArray();
            }

            Visibility = connectorType.Visibility.ToString();

            if (connectorType.DynamicList != null)
            {
                DynamicList = new YamlDynamicList()
                {
                    OperationId = connectorType.DynamicList.OperationId,
                    ItemValuePath = connectorType.DynamicList.ItemValuePath,
                    ItemPath = connectorType.DynamicList.ItemPath,
                    ItemTitlePath = connectorType.DynamicList.ItemTitlePath,
                    Map = GetMap(connectorType.DynamicList.ParameterMap)
                };
            }

            if (connectorType.DynamicProperty != null)
            {
                DynamicProperty = new YamlDynamicProperty()
                {
                    OperationId = connectorType.DynamicProperty.OperationId,
                    ItemValuePath = connectorType.DynamicProperty.ItemValuePath,
                    Map = GetMap(connectorType.DynamicProperty.ParameterMap)
                };
            }

            if (connectorType.DynamicSchema != null)
            {
                DynamicSchema = new YamlDynamicSchema()
                {
                    OperationId = connectorType.DynamicSchema.OperationId,
                    ValuePath = connectorType.DynamicSchema.ValuePath,
                    Map = GetMap(connectorType.DynamicSchema.ParameterMap)
                };
            }

            if (connectorType.DynamicValues != null)
            {
                DynamicValues = new YamlDynamicValues()
                {
                    OperationId = connectorType.DynamicValues.OperationId,
                    ValueTitle = connectorType.DynamicValues.ValueTitle,
                    ValuePath = connectorType.DynamicValues.ValuePath,
                    ValueCollection = connectorType.DynamicValues.ValueCollection,
                    Map = GetMap(connectorType.DynamicValues.ParameterMap)
                };
            }

            Schema = (connectorType.Schema as SwaggerSchema)._schema.GetString();
        }

        public string Name;
        public string DisplayName;
        public string Description;
        public bool IsRequired;
        public YamlConnectorType[] Fields;
        public string FormulaType;
        public bool ExplicitInput;
        public bool IsOptionSet;
        public bool IsEnum;
        public YamlEnumValue[] EnumValues;
        public string Visibility;
        public YamlDynamicList DynamicList;
        public YamlDynamicProperty DynamicProperty;
        public YamlDynamicSchema DynamicSchema;
        public YamlDynamicValues DynamicValues;
        public string Schema;

        private Dictionary<string, YamlMapping> GetMap(Dictionary<string, IConnectorExtensionValue> dic)
        {
            return dic.ToDictionary(kvp => kvp.Key, kvp => GetIP(kvp.Value));
        }

        private YamlMapping GetIP(IConnectorExtensionValue cev)
        {
            if (cev is StaticConnectorExtensionValue scev)
            {
                return new YamlMapping()
                {
                    Value = scev.Value.ToExpression(),
                    Type = scev.Value.Type._type.ToString()
                };
            }

            if (cev is DynamicConnectorExtensionValue dcev)
            {
                return new YamlMapping()
                {
                    Reference = dcev.Reference
                };
            }

            throw new Exception("Invalid IConnectorExtensionValue");
        }
    }

    public class YamlEnumValue
    {
        public string DisplayName;
        public string Value;
        public string Type;
    }

    public class YamlDynamicList
    {
        public string OperationId;
        public string ItemValuePath;
        public string ItemPath;
        public string ItemTitlePath;
        public Dictionary<string, YamlMapping> Map;
    }

    public class YamlDynamicProperty
    {
        public string OperationId;
        public string ItemValuePath;
        public Dictionary<string, YamlMapping> Map;
    }

    public class YamlDynamicSchema
    {
        public string OperationId;
        public string ValuePath;
        public Dictionary<string, YamlMapping> Map;
    }

    public class YamlDynamicValues
    {
        public string OperationId;
        public string ValueTitle;
        public string ValuePath;
        public string ValueCollection;
        public Dictionary<string, YamlMapping> Map;
    }

    public class YamlMapping
    {
        public YamlMapping()
        {
        }

        // Static
        public string Value;
        public string Type;

        // Dynamic
        public string Reference;
    }

    public static class Exts
    {
        public static string GetString(this OpenApiSchema schema)
        {
            StringBuilder sb = new StringBuilder();
            schema.GetStringInternal(new ConnectorTypeGetterSettings(0), sb);
            return sb.ToString();
        }

        private static void GetStringInternal(this OpenApiSchema schema, ConnectorTypeGetterSettings ctgs, StringBuilder sb)
        {
            if (ctgs.Level > 32)
            {
                sb.Append("<TooManyLevels>");
                return;
            }

            sb.Append(schema.Type);

            if (!string.IsNullOrEmpty(schema.Format))
            {
                sb.Append('.');
                sb.Append(schema.Format);
            }

            if (schema.Enum != null && schema.Enum.Any())
            {
                sb.Append($"[en:{schema.Enum.First().GetType().Name}]");
            }

            if (schema.Items != null)
            {
                sb.Append($"[it:");

                var itemIdentifier = OpenApiExtensions.GetUniqueIdentifier(SwaggerSchema.New(schema.Items));
                if (itemIdentifier.StartsWith("R:", StringComparison.Ordinal) && ctgs.Chain.Contains(itemIdentifier))
                {
                    sb.Append($"<circularRef:{itemIdentifier.Substring(2)}>]");
                    return;
                }

                GetStringInternal(schema.Items, ctgs.Stack(itemIdentifier), sb);
                ctgs.UnStack();
                sb.Append(']');
            }

            string discriminator = schema.Items?.Discriminator?.PropertyName;
            if (discriminator != null)
            {
                sb.Append($"[di:{discriminator}]");
            }

            if (schema.AdditionalProperties != null)
            {
                sb.Append($"[ad:");
                var additionalPropIdentifier = OpenApiExtensions.GetUniqueIdentifier(SwaggerSchema.New(schema.AdditionalProperties));
                if (additionalPropIdentifier.StartsWith("R:", StringComparison.Ordinal) && ctgs.Chain.Contains(additionalPropIdentifier))
                {
                    sb.Append($"<circularRef:{additionalPropIdentifier.Substring(2)}>]");
                    return;
                }

                GetStringInternal(schema.AdditionalProperties, ctgs.Stack(additionalPropIdentifier), sb);
                ctgs.UnStack();
                sb.Append(']');
            }

            if (schema.Properties != null && schema.Properties.Any())
            {
                sb.Append($"[pr:");
                int i = 0;

                foreach (var prop in schema.Properties)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(prop.Key);
                    sb.Append(':');

                    var propIdentifier = OpenApiExtensions.GetUniqueIdentifier(SwaggerSchema.New(prop.Value));
                    if (propIdentifier.StartsWith("R:", StringComparison.Ordinal) && ctgs.Chain.Contains(propIdentifier))
                    {
                        sb.Append($"<circularRef:{propIdentifier.Substring(2)}>]");
                        return;
                    }

                    GetStringInternal(prop.Value, ctgs.Stack(propIdentifier), sb);
                    ctgs.UnStack();

                    i++;
                }

                sb.Append(']');
            }

            if (schema.Extensions != null && schema.Extensions.Any())
            {
                sb.Append($"[ex:{string.Join(", ", schema.Extensions.Keys)}]");
            }
        }

        public static void Dump(this object obj, ITestOutputHelper console)
        {
            if (obj is IEnumerable e && e.GetType().GetGenericArguments().Count() > 0)
            {
                console.WriteLine($"IEnumerable<{e.GetType().GetGenericArguments()[0].Name}>:");

                foreach (var item in e)
                {
                    console.WriteLine(item.ToString());
                }
            }
            else
            {
                console.WriteLine(obj.ToString());
            }
        }
    }
}

#endif
