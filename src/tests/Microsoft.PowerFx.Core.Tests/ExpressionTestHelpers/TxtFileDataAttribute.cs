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
        private readonly string _filePathCommon;
        private readonly string _filePathSpecific;
        private readonly string _engineName;
        private readonly bool _numberIsFloat;

        public TxtFileDataAttribute(string filePathCommon, string filePathSpecific, string engineName, bool numberIsFloat)
        {
            _filePathCommon = filePathCommon;
            _filePathSpecific = filePathSpecific;
            _engineName = engineName;
            _numberIsFloat = numberIsFloat;
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
                            parser.AddFile(_numberIsFloat, file);
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
            var executable = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var curDir = Path.GetFullPath(Path.GetDirectoryName(executable));
            var testDir = Path.Combine(curDir, filePath);
            return testDir;
        }
    }
}
