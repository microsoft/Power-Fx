// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string _filePathCommon;
        private readonly string _filePathSpecific;
        private readonly string _engineName;
        private readonly Dictionary<string, bool> _setup;

        public TxtFileDataAttribute(string filePathCommon, string filePathSpecific, string engineName, string setup)
        {
            _filePathCommon = filePathCommon;
            _filePathSpecific = filePathSpecific;
            _engineName = engineName;
            _setup = TestRunner.ParseSetupString(setup);
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // This is run in a separate process. To debug, need to call Launch() and attach a debugger.
            // System.Diagnostics.Debugger.Launch();

            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var list = new List<object[]>();

            try
            {
                var parser = new TestRunner();

                foreach (var dir in new string[] { _filePathCommon, _filePathSpecific })
                {
                    if (dir != null)
                    {
                        var allFiles = Directory.EnumerateFiles(GetDefaultTestDir(dir));

                        foreach (var file in allFiles)
                        {
                            // Skip .md files

                            if (file.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase))
                            {
                                parser.AddFile(_setup, file);
                            }
                        }
                    }
                }

                foreach (var test in parser.Tests)
                {
                    var item = new ExpressionTestCase(_engineName, test);

                    list.Add(new object[] { item });
                }
            }
            catch (Exception e)
            {
                // If this method crashes, then we just get 0 tests. 
                // The only way to communicate a failure from here back to the developer
                // is to pass a "fake" test object that always fails and contains the error. 

                var item = ExpressionTestCase.Fail($"Test discovery failed with: {e}");

                list.Add(new object[] { item });
            }

            return list;
        }

        internal static string GetDefaultTestDir(string filePath)
        {
#pragma warning disable SYSLIB0012 // 'Assembly.CodeBase' is obsolete
            var executable = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
#pragma warning restore SYSLIB0012 // 'Assembly.CodeBase' is obsolete

            var curDir = Path.GetFullPath(Path.GetDirectoryName(executable));
            var testDir = Path.Combine(curDir, filePath);
            return testDir;
        }
    }

    /// <summary>
    /// Simpler version of TxtFileDataAttribute (above) for the mutation tests.
    /// The mutation tests need to run tests, one after another, retaining state - they are not independent.
    /// This attribute just gathers together the directory of test files, rather than pulling the individual tests out of those files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ReplFileSimpleListAttribute : DataAttribute
    {
        private readonly string _filePath;

        public ReplFileSimpleListAttribute(string filePath)
        {
            _filePath = filePath;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // This is run in a separate process. To debug, need to call Launch() and attach a debugger.
            // System.Diagnostics.Debugger.Launch();

            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var list = new List<object[]>();

            try
            {
                var path = TxtFileDataAttribute.GetDefaultTestDir(_filePath);
                var dir = new DirectoryInfo(path);
                var allFiles = dir.EnumerateFiles("*.txt");      // skip .md files

                foreach (var file in allFiles)
                {
                    list.Add(new object[] { file.Name });
                }
            }
            catch (Exception e)
            {
                // If this method crashes, then we just get 0 tests. 
                // The only way to communicate a failure from here back to the developer
                // is to pass a "fake" test object that always fails and contains the error.
                // In this case, this will be a bogus file name that won't load.

                var item = $"ERROR: Test discovery failed with: {e}";

                list.Add(new object[] { item });
            }

            return list;
        }
    }
}
