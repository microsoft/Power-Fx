// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
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
            _setup = ParseSetupString(setup);
        }

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

            possible.Add("PowerFxV1");
            possible.Add("DisableMemChecks");
            possible.Add("TimeZoneInfo");
            possible.Add("MutationFunctionsTestSetup");
            possible.Add("OptionSetTestSetup");
            possible.Add("AsyncTestSetup");
            possible.Add("OptionSetSortTestSetup");
            possible.Add("AllEnumsSetup");
            possible.Add("Default");

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
            var executable = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var curDir = Path.GetFullPath(Path.GetDirectoryName(executable));
            var testDir = Path.Combine(curDir, filePath);
            return testDir;
        }
    }
}
