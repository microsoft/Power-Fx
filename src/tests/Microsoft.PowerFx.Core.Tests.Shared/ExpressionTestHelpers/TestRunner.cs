// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Parse test files and invoke runners to execute them. 
    /// </summary>
    public class TestRunner
    {
        private readonly BaseRunner[] _runners;

        // Mapping of a Test's key to the test case in Test list.
        // Used for when we need to update the test. 
        private readonly Dictionary<string, TestCase> _keyToTests = new Dictionary<string, TestCase>(StringComparer.Ordinal);

        // Expose tests so that host can manipulate list directly. 
        // Also populate this by calling various Add*() functions to parse. 
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        // Files that have been disabled. 
        public HashSet<string> DisabledFiles = new HashSet<string>();

        public TestRunner(params BaseRunner[] runners)
        {
            _runners = runners;
        }

        public static string GetDefaultTestDir()
        {
            var curDir = Path.GetDirectoryName(typeof(TestRunner).Assembly.Location);
            var testDir = Path.Combine(curDir, "ExpressionTestCases");
            return testDir;
        }

        public string TestRoot { get; set; } = GetDefaultTestDir();

        // Parses a comma delimited setup string, as found in TxtFileDataAttributes and the start of .txt files,
        // into a dictionary for passing to AddDir, AddFile, etc.  This routine is used both to determine what
        // the current testing context supports (with TxtFileDataAtrributes) and what a given .txt file requires
        // (with AddFile).  These two dictionaries must be compatible and not contradict for a test to run.
        //
        // Dictionary contents, with NumberIsFloat as an example:
        //    <NumberIsFloat, true> = "NumberIsFloat" was specified
        //    <NumberIsFloat, false> = "disable:NumberIsFloat" was specified
        //
        // Use "Default" for all settings not explicilty called out.  Without Default, if a setting is not
        // specified, the test can be run with or without the setting.
        //
        // Setting strings are validated by here.  Any of these are possible choices:
        //    * Engine.Features, determined through reflection
        //    * TexlParser.Flags, determined through reflection
        //    * Default, special case
        //    * PowerFxV1, special case, will expand to its constituent Features
        //    * Other handlers listed in this routine
        public static Dictionary<string, bool> ParseSetupString(string setup)
        {
            var settings = new Dictionary<string, bool>();
            var possible = new HashSet<string>();
            var powerFxV1 = new Dictionary<string, bool>();

            // Features
            foreach (var featureProperty in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (featureProperty.PropertyType == typeof(bool) && featureProperty.CanWrite)
                {
                    possible.Add(featureProperty.Name);
                    if ((bool)featureProperty.GetValue(Features.PowerFxV1))
                    {
                        powerFxV1.Add(featureProperty.Name, true);
                    }
                }
            }

            // Parser Flags
            foreach (var parserFlag in System.Enum.GetValues(typeof(TexlParser.Flags)))
            {
                possible.Add(parserFlag.ToString());
            }

            possible.Add("AllEnumsSetup");
            possible.Add("AllEnumsPlusTestEnumsSetup");
            possible.Add("AsyncTestSetup");
            possible.Add("Blob");
            possible.Add("DecimalSupport");
            possible.Add("Default");
            possible.Add("DisableMemChecks");
            possible.Add("EnableJsonFunctions");
            possible.Add("MutationFunctionsTestSetup");
            possible.Add("OptionSetSortTestSetup");
            possible.Add("OptionSetTestSetup");
            possible.Add("PowerFxV1");
            possible.Add("RegEx");
            possible.Add("TimeZoneInfo");
            possible.Add("TraceSetup");

            foreach (Match match in Regex.Matches(setup, @"(disable:)?(([\w]+|//)(\([^\)]*\))?)"))
            {
                bool enabled = !(match.Groups[1].Value == "disable:");
                var name = match.Groups[3].Value;
                var complete = match.Groups[2].Value;

                // end of line comment on settings string
                if (name == "//")
                {
                    break;
                }

                if (!possible.Contains(name))
                {
                    throw new ArgumentException($"Setup string not found: {name} from \"{setup}\"");
                }

                settings.Add(complete, enabled);

                if (match.Groups[2].Value == "PowerFxV1")
                {
                    foreach (var pfx1Feature in powerFxV1)
                    {
                        settings.Add(pfx1Feature.Key, true);
                    }
                }
            }

            return settings;
        }

        public void AddDir(Dictionary<string, bool> setup, string directory = "")
        {
            directory = Path.GetFullPath(directory, TestRoot);
            var allFiles = Directory.EnumerateFiles(directory);

            AddFile(setup, allFiles);
        }

        public void AddFile(Dictionary<string, bool> setup, params string[] files)
        {
            var x = (IEnumerable<string>)files;
            AddFile(setup, x);
        }

        public void AddFile(Dictionary<string, bool> setup, IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                AddFile(setup, file);
            }
        }

        // Directive should start with #, end in : like "#SETUP:"
        // Returns true if matched; false if not. Throws on error.
        private static bool TryParseDirective(string line, string directive, out string param)
        {
            if (line.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
            {
                param = line.Substring(directive.Length).Trim();

                // strip any end of line comment
                if (param.Contains("//"))
                {
                    param = param.Substring(0, param.IndexOf("//"));
                }

                return true;
            }
            else
            {
                param = null;
                return false;
            }
        }

        public void AddFile(Dictionary<string, bool> setup, string thisFile)
        {
            thisFile = Path.GetFullPath(thisFile, TestRoot);

            var lines = File.ReadAllLines(thisFile);

            // Skip blanks or "comments"
            // >> indicates input expression
            // next line is expected result.

            Exception ParseError(int lineNumber, string message) => new InvalidOperationException(
                $"{Path.GetFileName(thisFile)} {lineNumber}: {message}");

            TestCase test = null;

            string fileSetup = null;
            string fileOveride = null;
            Dictionary<string, bool> fileSetupDict = new Dictionary<string, bool>();

            var i = -1;

            while (i < lines.Length - 1)
            {
                var line = lines[i + 1];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    i++;
                    continue;
                }

                if (line.Length > 1 && line[0] == '#')
                {
                    if (TryParseDirective(line, "#DISABLE:", out var fileDisable))
                    {
                        DisabledFiles.Add(fileDisable);

                        // Will remove all cases in this file.
                        // Can apply to multiple files. 
                        var countRemoved = Tests.RemoveAll(test => string.Equals(Path.GetFileName(test.SourceFile), fileDisable, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (TryParseDirective(line, "#SETUP:", out var thisSetup))
                    {
                        foreach (var flag in ParseSetupString(thisSetup))
                        {
                            if (fileSetupDict.ContainsKey(flag.Key) && fileSetupDict[flag.Key] != flag.Value)
                            {
                                // Multiple setup lines are fine, but can't contradict.
                                // ParseSetupString may expand aggregate handlers, such as PowerFxV1, which may create unexpected contradictions.
                                throw new InvalidOperationException($"Duplicate and contradictory #SETUP directives: {line} {(flag.Value ? string.Empty : "disable:")}{flag.Key}");
                            }
                            else
                            {
                                fileSetupDict.Add(flag.Key, flag.Value);
                            }
                        }
                    }
                    else if (TryParseDirective(line, "#OVERRIDE:", out var thisOveride))
                    {
                        if (fileOveride != null)
                        {
                            throw new InvalidOperationException($"Can't have multiple #OVERRIDE: directives");
                        }

                        fileOveride = thisOveride;
                    }
                    else
                    {
                        throw ParseError(i, $"Unrecognized directive: {line}");
                    }

                    i++;
                    continue;
                }

                break;
            }

            // If the test is incompatible with the base setup, skip it.
            // It is OK for the test to turn on handlers and features that don't conflict.
            foreach (var flag in fileSetupDict)
            {
                if ((setup.ContainsKey(flag.Key) && flag.Value != setup[flag.Key]) ||
                    (!setup.ContainsKey(flag.Key) && setup.ContainsKey("Default") && flag.Value != setup["Default"]))
                {
                    return;
                }
            }

            fileSetup = string.Join(",", fileSetupDict.Select(i => (i.Value ? string.Empty : "disable:") + i.Key));

            List<string> duplicateTests = new List<string>();

            while (true)
            {
                i++;
                if (i == lines.Length)
                {
                    break;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith(">>"))
                {
                    if (test != null)
                    {
                        throw ParseError(i, $"parse error- multiple test inputs in a row. Previous input is: {test.Input}");
                    }

                    line = line.Substring(2).Trim();
                    test = new TestCase
                    {
                        Input = line,
                        SourceLine = i + 1, // 1-based
                        SourceFile = thisFile,
                        SetupHandlerName = fileSetup
                    };
                    continue;
                }

                if (test != null)
                {
                    // If it's indented, then part of previous line. 
                    if (line[0] == ' ')
                    {
                        test.Input += "\r\n" + line;
                        continue;
                    }

                    // Line after the input is the response

                    // handle engine-specific results
                    if (line.StartsWith("/*"))
                    {
                        throw ParseError(i, $"Multiline comments aren't supported in output");
                    }

                    test.Expected = line.Trim();

                    var key = test.GetUniqueId(fileOveride);
                    if (_keyToTests.TryGetValue(key, out var existingTest))
                    {
                        // Must be in different sources
                        if (existingTest.SourceFile == test.SourceFile && existingTest.SetupHandlerName == test.SetupHandlerName)
                        {
                            duplicateTests.Add($"Duplicate test cases in {Path.GetFileName(test.SourceFile)} on line {test.SourceLine} and {existingTest.SourceLine}");
                        }

                        // Updating an existing test. 
                        // Inputs are the same, but update the results.
                        existingTest.MarkOverride(test);
                    }
                    else
                    {
                        // New test
                        Tests.Add(test);

                        _keyToTests[key] = test;
                    }

                    test = null;
                }
                else
                {
                    throw ParseError(i, $"Parse error");
                }
            }

            if (test != null)
            {
                throw ParseError(i, "Parse error - missing test result");
            }

            if (duplicateTests.Any())
            {
                throw ParseError(0, string.Join("\r\n", duplicateTests));
            }
        }

        public TestRunFullResults RunTests(bool numberIsFloat = false)
        {
            var summary = new TestRunFullResults();

            if (_runners.Length == 0)
            {
                throw new InvalidOperationException($"Need to specify a runner to run tests");
            }

            foreach (var testCase in Tests)
            {
                foreach (var runner in _runners)
                {
                    var engineName = runner.GetName();

                    var (result, msg) = runner.RunTestCase(testCase);

                    summary.AddResult(testCase, result, engineName, msg);
                }
            }

            return summary;
        }
    }
}
