// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

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
                        throw new InvalidOperationException($"Unrecognized directive: {line}");
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
                        throw new InvalidOperationException($"Multiline comments aren't supported in output");                        
                    }

                    test.Expected = line.Trim();

                    var key = test.GetUniqueId(fileOveride);
                    if (_keyToTests.TryGetValue(key, out var existingTest))
                    {
                        // Must be in different sources
                        if (existingTest.SourceFile == test.SourceFile)
                        {
                            throw new InvalidOperationException($"Duplicate test cases in {Path.GetFileName(test.SourceFile)} on line {test.SourceLine} and {existingTest.SourceLine}");
                        }
                        
                        // Updating an existing test. 
                        // Inputs are the same, but update the results.
                        existingTest.Expected = test.Expected;
                        existingTest.SourceFile = test.SourceFile;
                        existingTest.SourceLine = test.SourceLine;
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
                    throw new InvalidOperationException($"Parse error at {Path.GetFileName(thisFile)} on line {i}");
                }
            }

            if (test != null)
            {
                throw new InvalidOperationException($"Parse error at {Path.GetFileName(thisFile)} on line {i}, missing test result");
            }
        }

        public (int total, int failed, int passed, string output) RunTests()
        {
            if (_runners.Length == 0)
            {
                throw new InvalidOperationException($"Need to specify a runner to run tests");
            }

            var total = 0;
            var fail = 0;
            var pass = 0;
            var sb = new StringBuilder();

            foreach (var testCase in Tests)
            {
                foreach (var runner in _runners)
                {
                    total++;

                    var engineName = runner.GetName();

                    var (result, msg) = runner.RunAsync(testCase).Result;

                    var prefix = $"Test {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}: ";
                    switch (result)
                    {
                        case TestResult.Pass:
                            pass++;
                            sb.Append(".");
                            break;

                        case TestResult.Fail:
                            sb.AppendLine();
                            sb.AppendLine($"FAIL: {engineName}, {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}");
                            sb.AppendLine($"FAIL: {testCase.Input}");
                            sb.AppendLine($"{msg}");
                            sb.AppendLine();
                            fail++;
                            break;

                        case TestResult.Skip:
                            sb.Append("-");
                            break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"{total} total. {pass} passed. {fail} failed");
            Console.WriteLine(sb.ToString());
            return (total, fail, pass, sb.ToString());
        }

        public static string TestToString(FormulaValue result)
        {
            var sb = new StringBuilder();
            try
            {
                TestToString(result, sb);
            }
            catch (Exception e)
            {
                // This will cause a diff and test failure below. 
                sb.Append($"<exception writing result: {e.Message}>");
            }

            var actualStr = sb.ToString();
            return actualStr;
        }

        // $$$ Move onto FormulaValue. 
        // Result here should be a string value that could be parsed. 
        // Normalize so we can use this in test cases. 
        internal static void TestToString(FormulaValue result, StringBuilder sb)
        {
            if (result is NumberValue n)
            {
                sb.Append(n.Value);
            }
            else if (result is BooleanValue b)
            {
                sb.Append(b.Value ? "true" : "false");
            }
            else if (result is StringValue s)
            {
                // $$$ proper escaping?
                sb.Append('"' + s.Value + '"');
            }
            else if (result is TableValue t)
            {
                var tableType = (TableType)t.Type;
                var canUseSquareBracketSyntax = t.IsColumn && t.Rows.All(r => r.IsValue) && tableType.GetNames().First().Name == "Value";
                if (canUseSquareBracketSyntax)
                {
                    sb.Append('[');
                }
                else
                {
                    sb.Append("Table(");
                }

                var dil = string.Empty;
                foreach (var row in t.Rows)
                {
                    sb.Append(dil);
                    dil = ",";

                    if (canUseSquareBracketSyntax)
                    {
                        var val = row.Value.Fields.First().Value;
                        TestToString(val, sb);
                    }
                    else
                    {
                        if (row.IsValue)
                        {
                            TestToString(row.Value, sb);
                        }
                        else
                        {
                            TestToString(row.ToFormulaValue(), sb);
                        }
                    }

                    dil = ",";
                }

                if (canUseSquareBracketSyntax)
                {
                    sb.Append(']');
                }
                else
                {
                    sb.Append(')');
                }
            }
            else if (result is RecordValue r)
            {
                var fields = r.Fields.ToArray();
                Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

                sb.Append('{');
                var dil = string.Empty;

                foreach (var field in fields)
                {
                    sb.Append(dil);
                    sb.Append(field.Name);
                    sb.Append(':');
                    TestToString(field.Value, sb);

                    dil = ",";
                }

                sb.Append('}');
            }
            else if (result is BlankValue)
            {
                sb.Append("Blank()");
            }
            else if (result is DateValue d)
            {
                // Date(YYYY,MM,DD)
                var date = d.Value;
                sb.Append($"Date({date.Year},{date.Month},{date.Day})");
            }
            else if (result is DateTimeValue dt)
            {
                // DateTime(yyyy,MM,dd,HH,mm,ss,fff)
                var dateTime = dt.Value;
                sb.Append($"DateTime({dateTime.Year},{dateTime.Month},{dateTime.Day},{dateTime.Hour},{dateTime.Minute},{dateTime.Second},{dateTime.Millisecond})");
            }
            else if (result is ErrorValue)
            {
                sb.Append(result);
            }
            else
            {
                throw new InvalidOperationException($"unsupported value type: {result.GetType().Name}");
            }
        }
    }
}
