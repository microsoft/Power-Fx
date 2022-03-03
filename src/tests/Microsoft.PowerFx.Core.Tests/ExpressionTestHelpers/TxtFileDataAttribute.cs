// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Xunit adapter to generate a single xunit test per case in the .txt file.
    /// Note that this is run in a separate process, and then generates a serialized list of test cases. 
    /// So breakpoints set in here will not be hit with regular F5 debugging. 
    /// </summary>
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
            thisFile = Path.GetFullPath(thisFile, GetDefaultTestDir());

            // Get the absolute path to the .txt file
            var path = Path.IsPathRooted(thisFile)
                ? thisFile
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), thisFile);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {thisFile}");
            }

            var tests = new List<ExpressionTestCase>();

            var parser = new TestRunner();
            parser.AddFile(path);

            foreach (var test in parser.Tests)
            {
                tests.Add(new ExpressionTestCase(_engineName, test));
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
            return GetDefaultTestDir(_filePath);
        }

        internal static string GetDefaultTestDir(string filePath)
        { 
            var executable = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var curDir = Path.GetFullPath(Path.GetDirectoryName(executable));
            var testDir = Path.Combine(curDir, filePath);
            return testDir;
        }
    }
}
