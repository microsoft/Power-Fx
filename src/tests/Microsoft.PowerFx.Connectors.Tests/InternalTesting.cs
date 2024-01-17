// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.TexlFunctionExporter;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization;

namespace Microsoft.PowerFx.Connectors.Tests
{
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

        private (string outFolder, string srcFolder) GetFolders()
        {
            string outFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\.."));
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
                        supportedFunctionList = string.Join(", ", m2.Groups[2].Value.Split(",").Select(x => x.Trim()).OrderBy(x => x));
                        result = string.Empty;
                    }

                    if (ok && !allFunctionsSupported && !noSupported)
                    {
                        // @"OK - ([0-9]+) supported functions \[([^\]]+)\], ([0-9]+) not supported functions(.*)"
                        Match m3 = rex3.Match(result);
                        supportedFunctions = int.Parse(m3.Groups[1].Value);
                        supportedFunctionList = string.Join(", ", m3.Groups[2].Value.Split(",").Select(x => x.Trim()).OrderBy(x => x));

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
                                if (w2.ContainsKey(title))
                                {
                                    w2[title].Add(s);
                                }
                                else
                                {
                                    w2.Add(title, new List<string>() { s });
                                }
                            }
                        }

                        dynamic obj = cf1.ToExpando(swaggerFile);
                        var serializer = new SerializerBuilder().Build();
                        string yaml = serializer.Serialize(obj);

                        string functionFile = Path.Combine(cFolder, cf1.OriginalName.Replace("/", "_") + ".yaml");
                        File.WriteAllText(functionFile, yaml);
                    }
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
        [InlineData("Library")] // Default Power-Fx library
        [InlineData("Aapt-Ppc", 0, "apidefinition*swagger*.json", @"aapt\src", @"ppc")]
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
            ConnectorSettings connectorSettings = new ConnectorSettings("FakeNamespace")
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
                List<TexlFunction> texlFunctions = OpenApiParser.ParseInternal(connectorSettings, connector.Value.document, logger).texlFunctions.Cast<TexlFunction>().ToList();
                List<ConnectorFunction> connectorFunctions = OpenApiParser.ParseInternal(connectorSettings, connector.Value.document, logger).connectorFunctions.Cast<ConnectorFunction>().ToList();

                // Step 3: Export TexlFunctions to Yaml
                ExportTexlFunctionsToYaml(reference, outFolderPath, connector.Key, texlFunctions, false);

                // Step 3: Export TexlFunctions to Yaml
                ExportConnectorFunctionsToYaml(reference, outFolderPath, connector.Key, connectorFunctions);
            }
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
            dynamic obj = connectorFunction.ToExpando(null);
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(obj);            

            string functionFile = Path.Combine(folder, "ConnectorFunction_" + connectorFunction.Name.Replace("/", "_", StringComparison.OrdinalIgnoreCase) + ".yaml");
            Directory.CreateDirectory(folder);

            if (File.Exists(functionFile))
            {
                throw new IOException($"File {functionFile} already exists!");
            }

            File.WriteAllText(functionFile, yaml, Encoding.UTF8);
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
            dynamic obj = connectorFunction.ToExpando(null);
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(obj);            

            string functionFile = Path.Combine(folder, "ConnectorFunction_" + connectorFunction.Name.Replace("/", "_", StringComparison.OrdinalIgnoreCase) + ".yaml");
            Directory.CreateDirectory(folder);

            if (File.Exists(functionFile))
            {
                throw new IOException($"File {functionFile} already exists!");
            }

            File.WriteAllText(functionFile, yaml, Encoding.UTF8);
        }
    }

    public static class Exts
    {
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

        public static ExpandoObject ToExpando(this ConnectorFunction connectorFunction, string swaggerFile)
        {
            dynamic func = new ExpandoObject();

            func.Name = connectorFunction.Name;
            func.OperationId = connectorFunction.OriginalName;
            func.Method = connectorFunction.HttpMethod.ToString().ToUpperInvariant();
            func.Path = connectorFunction.OperationPath;

            if (!string.IsNullOrEmpty(swaggerFile))
            {
                func.SwaggerFile = swaggerFile;
            }

            if (!string.IsNullOrEmpty(connectorFunction.Description))
            {
                func.Description = connectorFunction.Description;
            }

            if (!string.IsNullOrEmpty(connectorFunction.Summary))
            {
                func.Summary = connectorFunction.Summary;
            }

            func.IsBehavior = connectorFunction.IsBehavior;
            func.IsSupported = connectorFunction.IsSupported;
            func.NotSupportedReason = connectorFunction.NotSupportedReason;
            func.IsDeprecated = connectorFunction.IsDeprecated;
            func.IsInternal = connectorFunction.IsInternal;
            func.IsPageable = connectorFunction.IsPageable;

            if (connectorFunction.RequiresUserConfirmation)
            {
                func.RequiresUserConfirmation = connectorFunction.RequiresUserConfirmation;
            }

            if (!string.IsNullOrEmpty(connectorFunction.Visibility))
            {
                func.Visibility = connectorFunction.Visibility;
            }

            func.ReturnType = connectorFunction.ReturnType._type.ToString();
            func.ReturnType_Detailed = connectorFunction.ReturnParameterType == null ? (dynamic)"null" : connectorFunction.ReturnParameterType.ToExpando(noname: true);

            func.ArityMin = connectorFunction.ArityMin;
            func.ArityMax = connectorFunction.ArityMax;
            func.RequiredParameters = connectorFunction.RequiredParameters == null ? (dynamic)"null" : connectorFunction.RequiredParameters.Select(rp => rp.ToExpando()).ToList();
            func.OptionalParameters = connectorFunction.OptionalParameters == null ? (dynamic)"null" : connectorFunction.OptionalParameters.Select(op => op.ToExpando()).ToList();

            return func;
        }

        internal static ExpandoObject ToExpando(this ConnectorParameter connectorParam)
        {
            dynamic cParam = new ExpandoObject();

            cParam.Name = connectorParam.Name;

            if (!string.IsNullOrEmpty(connectorParam.Description))
            {
                cParam.Description = connectorParam.Description;
            }

            if (connectorParam.Location != null)
            {
                cParam.Location = connectorParam.Location.ToString();
            }

            cParam.FormulaType = connectorParam.FormulaType._type.ToString();
            cParam.Type = connectorParam.ConnectorType.ToExpando();

            if (connectorParam.DefaultValue != null)
            {
                StringBuilder sb = new StringBuilder();
                connectorParam.DefaultValue.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
                cParam.DefaultValue = sb.ToString();
            }

            if (!string.IsNullOrEmpty(connectorParam.Title))
            {
                cParam.Title = connectorParam.Title;
            }

            if (connectorParam.ConnectorExtensions.ExplicitInput)
            {
                cParam.ExplicitInput = connectorParam.ConnectorExtensions.ExplicitInput;
            }

            return cParam;
        }

        internal static ExpandoObject ToExpando(this ConnectorType connectorType, bool noname = false)
        {
            dynamic cType = new ExpandoObject();

            if (!noname)
            {
                cType.Name = connectorType.Name;

                if (!string.IsNullOrEmpty(connectorType.DisplayName))
                {
                    cType.DisplayName = connectorType.DisplayName;
                }

                if (!string.IsNullOrEmpty(connectorType.Description))
                {
                    cType.Description = connectorType.Description;
                }
            }

            cType.IsRequired = connectorType.IsRequired;

            if (connectorType.Fields != null && connectorType.Fields.Any())
            {
                cType.Fields = connectorType.Fields.Select(fieldCT => fieldCT.ToExpando()).ToList();
            }

            cType.FormulaType = connectorType.FormulaType._type.ToString();

            if (connectorType.ExplicitInput)
            {
                cType.ExplicitInput = connectorType.ExplicitInput;
            }

            cType.IsEnum = connectorType.IsEnum;

            if (connectorType.IsEnum)
            {
                bool hasDisplayNames = connectorType.EnumDisplayNames != null && connectorType.EnumDisplayNames.Length > 0;
                cType.EnumValues = connectorType.EnumValues.Select<FormulaValue, object>((ev, i) => GetEnumExpando(hasDisplayNames ? connectorType.EnumDisplayNames[i] : null, ev.ToObject().ToString(), ev.Type._type.ToString())).ToList();
            }

            if (connectorType.Visibility != Visibility.None && connectorType.Visibility != Visibility.Unknown)
            {
                cType.Visibility = connectorType.Visibility.ToString();
            }

            if (connectorType.DynamicList != null)
            {
                cType.DynamicList = connectorType.DynamicList.ToExpando();
            }

            if (connectorType.DynamicProperty != null)
            {
                cType.DynamicProperty = connectorType.DynamicProperty.ToExpando();
            }

            if (connectorType.DynamicSchema != null)
            {
                cType.DynamicSchema = connectorType.DynamicSchema.ToExpando();
            }

            if (connectorType.DynamicValues != null)
            {
                cType.DynamicValues = connectorType.DynamicValues.ToExpando();
            }

            return cType;
        }

        internal static ExpandoObject GetEnumExpando(string displayName, string value, string type)
        {
            dynamic e = new ExpandoObject();

            if (!string.IsNullOrEmpty(displayName))
            {
                e.DisplayName = displayName;
            }

            e.Value = value;
            e.Type = type;

            return e;
        }

        internal static ExpandoObject ToExpando(this ConnectorDynamicList dynamicList)
        {
            dynamic dList = new ExpandoObject();

            dList.OperationId = dynamicList.OperationId;

            if (!string.IsNullOrEmpty(dynamicList.ItemValuePath))
            {
                dList.ItemValuePath = dynamicList.ItemValuePath;
            }

            if (!string.IsNullOrEmpty(dynamicList.ItemPath))
            {
                dList.ItemPath = dynamicList.ItemPath;
            }

            if (!string.IsNullOrEmpty(dynamicList.ItemTitlePath))
            {
                dList.ItemTitlePath = dynamicList.ItemTitlePath;
            }

            if (dynamicList.ParameterMap != null)
            {
                dList.Map = dynamicList.ParameterMap.ToExpando();
            }

            return dList;
        }

        internal static ExpandoObject ToExpando(this ConnectorDynamicProperty dynamicProp)
        {
            dynamic dProp = new ExpandoObject();

            dProp.OperationId = dynamicProp.OperationId;

            if (!string.IsNullOrEmpty(dynamicProp.ItemValuePath))
            {
                dProp.ItemValuePath = dynamicProp.ItemValuePath;
            }

            if (dynamicProp.ParameterMap != null)
            {
                dProp.Map = dynamicProp.ParameterMap.ToExpando();
            }

            return dProp;
        }

        internal static ExpandoObject ToExpando(this ConnectorDynamicSchema dynamicSchema)
        {
            dynamic dSchema = new ExpandoObject();

            dSchema.OperationId = dynamicSchema.OperationId;

            if (!string.IsNullOrEmpty(dynamicSchema.ValuePath))
            {
                dSchema.ValuePath = dynamicSchema.ValuePath;
            }

            if (dynamicSchema.ParameterMap != null)
            {
                dSchema.Map = dynamicSchema.ParameterMap.ToExpando();
            }

            return dSchema;
        }

        internal static ExpandoObject ToExpando(this ConnectorDynamicValue dynamicValue)
        {
            dynamic dValue = new ExpandoObject();

            dValue.OperationId = dynamicValue.OperationId;

            if (!string.IsNullOrEmpty(dynamicValue.ValuePath))
            {
                dValue.ValuePath = dynamicValue.ValuePath;
            }

            if (!string.IsNullOrEmpty(dynamicValue.ValueTitle))
            {
                dValue.ValueTitle = dynamicValue.ValueTitle;
            }

            if (!string.IsNullOrEmpty(dynamicValue.ValueCollection))
            {
                dValue.ValueCollection = dynamicValue.ValueCollection;
            }

            if (dynamicValue.ParameterMap != null)
            {
                dValue.Map = dynamicValue.ParameterMap.ToExpando();
            }

            return dValue;
        }

        internal static ExpandoObject ToExpando(this Dictionary<string, IConnectorExtensionValue> paramMap)
        {
            IDictionary<string, object> dMap = new ExpandoObject() as IDictionary<string, object>;

            foreach (var kvp in paramMap)
            {
                if (kvp.Value is StaticConnectorExtensionValue scev)
                {
                    dMap[kvp.Key] = GetStaticExt(scev.Value.ToExpression(), scev.Value.Type._type.ToString());
                }
                else if (kvp.Value is DynamicConnectorExtensionValue dcev)
                {
                    dMap[kvp.Key] = GetDynamicExt(dcev.Reference);
                }
            }

            return (dynamic)dMap;
        }

        internal static ExpandoObject GetStaticExt(string value, string type)
        {
            dynamic s = new ExpandoObject();

            s.Value = value;
            s.Type = type;

            return s;
        }

        internal static ExpandoObject GetDynamicExt(string @ref)
        {
            dynamic s = new ExpandoObject();

            s.Reference = @ref;

            return s;
        }
    }
}
