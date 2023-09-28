// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Tests;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;

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
            string outFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\.."));
            string srcFolder = Path.GetFullPath(Path.Combine(outFolder, ".."));
            string reportName = @"report\Analysis.txt";
            string jsonReport = @"report\Report.json";

            // New report name every second
            string jsonReport2 = @$"report\Report_{Math.Round(DateTime.UtcNow.Ticks / 1e7):00000000000}.json";

            // On build servers: ENV: C:\__w\1\s\pfx\src\tests\Microsoft.PowerFx.Connectors.Tests\bin\Release\netcoreapp3.1
            // Locally         : ENV: C:\Data\Power-Fx\src\tests\Microsoft.PowerFx.Connectors.Tests\bin\Debug\netcoreapp3.1
            _output.WriteLine($"ENV: {Environment.CurrentDirectory}");
            _output.WriteLine($"OUT: {outFolder}");
            _output.WriteLine($"SRC: {srcFolder}");

            Directory.CreateDirectory(Path.Combine(outFolder, "report"));
            GenerateReport(reportName, outFolder, srcFolder);
            AnalyzeReport(reportName, outFolder, srcFolder, jsonReport);

            File.Copy(Path.Combine(outFolder, jsonReport), Path.Combine(outFolder, jsonReport2));
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

        private void GenerateReport(string reportName, string outFolder, string srcFolder)
        {
            int i = 0;
            int j = 0;
            using StreamWriter writer = new StreamWriter(Path.Combine(outFolder, reportName), append: false);

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
                    OpenApiDocument doc = Helpers.ReadSwagger(swaggerFile);
                    title = $"{doc.Info.Title} [{swaggerFile}]";

                    // Check we can get the functions
                    IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("C", doc, new ConsoleLogger(_output));

                    allFunctions.Add(title, functions);
                    var config = new PowerFxConfig();
                    using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token")
                    {
                        SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f"
                    };

                    // Check we can add the service (more comprehensive test)
                    config.AddActionConnector("Connector", doc, new ConsoleLogger(_output));
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
            IEnumerable<ConnectorFunction> functions = OpenApiParser.GetFunctions("C", doc, new ConsoleLogger(_output));

            var config = new PowerFxConfig();
            using var client = new PowerPlatformConnectorClient("firstrelease-001.azure-apim.net", "839eace6-59ab-4243-97ec-a5b8fcc104e4", "72c42ee1b3c7403c8e73aa9c02a7fbcc", () => "Some JWT token") { SessionId = "ce55fe97-6e74-4f56-b8cf-529e275b253f" };

            config.AddActionConnector("Connector", doc, new ConsoleLogger(_output));
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
    }
}
