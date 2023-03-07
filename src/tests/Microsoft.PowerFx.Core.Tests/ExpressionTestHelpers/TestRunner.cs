// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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

        private readonly List<Engine> _engines = new List<Engine>();

        // Files that have been disabled. 
        public HashSet<string> DisabledFiles = new HashSet<string>();

        public string[] Locales { get; set; } = new[] { "en-US" };

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

        public void AddDir(string directory = "")
        {
            directory = Path.GetFullPath(directory, TestRoot);
            var allFiles = Directory.EnumerateFiles(directory);

            AddFile(allFiles);
        }

        public void AddFile(params string[] files)
        {
            var x = (IEnumerable<string>)files;
            AddFile(x);
        }

        public void AddFile(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                AddFile(file);
            }
        }

        // Directive should start with #, end in : like "#SETUP:"
        // Returns true if matched; false if not. Throws on error.
        private static bool TryParseDirective(string line, string directive, ref string param)
        {
            if (line.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
            {
                if (param != null)
                {
                    throw new InvalidOperationException($"Can't have multiple {directive}");
                }

                param = line.Substring(directive.Length).Trim();
                return true;
            }

            return false;
        }

        public void AddFile(string thisFile)
        {
            thisFile = Path.GetFullPath(thisFile, TestRoot);

            var lines = File.ReadAllLines(thisFile);

            // Skip blanks or "comments"
            // >> indicates input expression
            // next line is expected result.

            Exception ParseError(int lineNumber, string message) => new InvalidOperationException($"{Path.GetFileName(thisFile)} {lineNumber}: {message}");

            TestCase test = null;

            var i = -1;

            // Preprocess file directives
            // #Directive: Parameter
            string fileSetup = null;
            string fileOveride = null;

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
                    string fileDisable = null;
                    if (TryParseDirective(line, "#DISABLE:", ref fileDisable))
                    {
                        DisabledFiles.Add(fileDisable);

                        // Will remove all cases in this file.
                        // Can apply to multiple files. 
                        var countRemoved = Tests.RemoveAll(test => string.Equals(Path.GetFileName(test.SourceFile), fileDisable, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (TryParseDirective(line, "#SETUP:", ref fileSetup) ||
                             TryParseDirective(line, "#OVERRIDE:", ref fileOveride))
                    {
                        // flag is set, no additional work needed.
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
                        throw ParseError(i, $"parse error - multiple test inputs in a row. Previous input is: {test.Input}");
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

                    foreach (string locale in Locales)
                    {
                        Engine e = _engines.FirstOrDefault(e => e.Config.CultureInfo.Name == locale);

                        if (e == null)
                        {
                            e = new Engine(new PowerFxConfig(new CultureInfo(locale)));
                            _engines.Add(e);
                        }

                        TestCase testWithLocale = new TestCase()
                        {
                            Input = (locale == "en-US") ? test.Input : e.GetDisplayExpression(test.Input, null as ReadOnlySymbolTable),
                            SourceFile = test.SourceFile,
                            SourceLine = test.SourceLine,
                            SetupHandlerName = test.SetupHandlerName,
                            Expected = test.Expected, // Never adapted to locale
                            Locale = locale
                        };

                        var key = testWithLocale.GetUniqueId(fileOveride);

                        if (_keyToTests.TryGetValue(key, out var existingTest))
                        {
                            // Must be in different sources & locales
                            if (existingTest.SourceFile == testWithLocale.SourceFile && existingTest.SetupHandlerName == testWithLocale.SetupHandlerName && existingTest.Locale == testWithLocale.Locale)
                            {
                                throw ParseError(i, $"Duplicate test cases in {Path.GetFileName(testWithLocale.SourceFile)} on line {testWithLocale.SourceLine} [{testWithLocale.Locale}] and {existingTest.SourceLine} [{existingTest.Locale}]");
                            }

                            // Updating an existing test. 
                            // Inputs are the same, but update the results.
                            existingTest.MarkOverride(testWithLocale);
                        }
                        else
                        {
                            // New test
                            Tests.Add(testWithLocale);

                            _keyToTests[key] = testWithLocale;
                        }
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
        }

        public TestRunFullResults RunTests()
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
