// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Core.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class TxtFileDataAttribute : DataAttribute
    {
        private readonly string _filePath;
        private readonly string _engineName;

        public TxtFileDataAttribute(string filePath, string engineName)
        {
            _filePath = filePath;
            _engineName = engineName;
        }

        public List<ExpressionTestCase> GetTestsFromFile(string thisFile)
        {
            var tests = new List<ExpressionTestCase>();
            thisFile = Path.GetFullPath(thisFile, GetDefaultTestDir());

            // Get the absolute path to the .txt file
            var path = Path.IsPathRooted(thisFile)
                ? thisFile
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), thisFile);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {thisFile}");
            }

            var lines = File.ReadAllLines(path);

            // Skip blanks or "comments"
            // >> indicates input expression
            // next line is expected result.

            ExpressionTestCase test = null;

            var i = -1;

            // Preprocess file directives
            string fileSetup = null;
            
            while (true)
            {
                i++;
                if (i == lines.Length)
                {
                    break;
                }

                var line = lines[i];

                if (line.StartsWith("#SETUP:"))
                {
                    fileSetup = line.Substring("#SETUP:".Length).Trim();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith(">>"))
                {
                    line = line.Substring(2).Trim();
                    test = new ExpressionTestCase(_engineName)
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
                        var index = line.IndexOf("*/");
                        if (index > -1)
                        {
                            var engine = line.Substring(2, index - 2).Trim();
                            var result = line.Substring(index + 2).Trim();
                            test.SetExpected(result, engine);
                            continue;
                        }
                    }

                    test.SetExpected(line.Trim());

                    tests.Add(test);
                    test = null;
                }
            }

            return tests;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var allFiles = Directory.EnumerateFiles(GetDefaultTestDir());
            var tests = new List<ExpressionTestCase>();
            foreach (var file in allFiles)
            {
                tests.AddRange(GetTestsFromFile(file));
            }

            foreach (var item in tests)
            {
                yield return new object[1] { item };
            }
        }

        private string GetDefaultTestDir()
        {
            var executable = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var curDir = Path.GetFullPath(Path.GetDirectoryName(executable));
            var testDir = Path.Combine(curDir, _filePath);
            return testDir;
        }
    }
}
