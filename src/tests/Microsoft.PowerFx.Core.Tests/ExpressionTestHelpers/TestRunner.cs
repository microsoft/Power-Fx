// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

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
                if (param.LastIndexOf("//") >= 0)
                {
                    param = param.Substring(0, param.LastIndexOf("//"));
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
                        foreach (var flag in TxtFileDataAttribute.ParseSetupString(thisSetup))
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
