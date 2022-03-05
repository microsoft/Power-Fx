// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    ///  Describe a test case from the .txt file.
    /// </summary>
    public class TestCase
    {
        // Formula string to run 
        public string Input;

        // Expected Result, indexed by runner name
        public Dictionary<string, string> _expected = new Dictionary<string, string>();

        // Location from source file. 
        public string SourceFile;
        public int SourceLine;
        public string SetupHandlerName;

        // Uniquely identity this test case. 
        // This is very useful for when another file needs to override the results. 
        public string GetUniqueId(string file)
        {
            // Inputs are case sensitive, so the overall key must be case sensitive. 
            // But filenames are case insensitive, so canon them to lowercase.
            var fileKey = file ?? Path.GetFileName(SourceFile);
            
            return fileKey.ToLower() + ":" + Input;
        }

        public override string ToString()
        {
            return $"{Path.GetFileName(SourceFile)}:{SourceLine}: {Input}";
        }

        public void SetExpected(string expected, string engineName = null)
        {
            if (engineName == null)
            {
                engineName = "-";
            }

            _expected[engineName] = expected;
        }

        public string GetExpected(string engineName)
        {
            if (!_expected.TryGetValue(engineName, out var expected))
            {
                return _expected["-"];
            }

            return expected;
        }
    }
}
