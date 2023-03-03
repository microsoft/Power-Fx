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
        public string Expected;

        // Location from source file. 
        public string SourceFile;
        public int SourceLine;
        public string SetupHandlerName;

        // For diagnostics, save the orginal location
        public string OverrideFrom;
        public string Locale;

        public bool IsOverride => OverrideFrom != null;

        // Mark that the test is getting overriden with a new expected result. 
        // This enables per-engine customizations.
        public void MarkOverride(TestCase newTest)
        {
            OverrideFrom = $"{newTest.SourceFile}:{newTest.SourceLine}";
            Expected = newTest.Expected;
            SourceFile = newTest.SourceFile;
            SourceLine = newTest.SourceLine;
        }

        // Uniquely identity this test case. 
        // This is very useful for when another file needs to override the results. 
        public string GetUniqueId(string file)
        {
            // Inputs are case sensitive, so the overall key must be case sensitive. 
            // But filenames are case insensitive, so canon them to lowercase.
            var fileKey = file ?? Path.GetFileName(SourceFile);

            return fileKey.ToLower() + ":" + (string.IsNullOrEmpty(Locale) ? string.Empty : $" [{Locale}] ") + Input;
        }

        public override string ToString()
        {
            return $"{Path.GetFileName(SourceFile)}:{SourceLine}: [{(string.IsNullOrEmpty(Locale) ? "-" : Locale)}] {Input}";
        }
    }
}
