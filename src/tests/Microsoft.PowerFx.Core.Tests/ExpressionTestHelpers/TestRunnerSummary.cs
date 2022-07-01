// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.PowerFx.Core.Tests
{
    [DebuggerDisplay("{Pass} passed, {Fail} failed, {Skip} skipped.")]
    public class TestRunFullResults
    {
        public int Total { get; set; }

        public int Pass { get; set; }

        public int Fail { get; set; }

        public int Skip { get; set; }

        public string Output => _output.ToString() + $"\r\n{Total} total. {Pass} passed. {Fail} failed";

        public void AddResult(TestCase testCase, TestResult result, string engineName, string message)
        {
            Total++;

            var file = Path.GetFileName(testCase.SourceFile);
            switch (result)
            {
                case TestResult.Pass:
                    Pass++;
                    _output.Append('.');
                    break;

                case TestResult.Fail:
                    if (file != null) 
                    {
                        PerFileFailure.TryGetValue(file, out var count);
                        count++;
                        PerFileFailure[file] = count;
                    }

                    _output.AppendLine();
                    _output.AppendLine($"FAIL: {engineName}, {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}");
                    _output.AppendLine($"FAIL: {testCase.Input}");
                    _output.AppendLine($"{message}");
                    _output.AppendLine();
                    Fail++;
                    break;

                case TestResult.Skip:
                    _output.Append('-');
                    break;
            }
        }

        /// <summary>
        /// Count failures per file.
        /// </summary>
        public Dictionary<string, int> PerFileFailure = new Dictionary<string, int>();

        private readonly StringBuilder _output = new StringBuilder();
    }
}
