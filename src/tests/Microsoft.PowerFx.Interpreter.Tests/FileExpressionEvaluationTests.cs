// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests : PowerFxTest
    {
        private InterpreterRunner _runner;

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner))]
        public void InterpreterTestCase(ExpressionTestCase testCase)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over. 
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            _runner = new InterpreterRunner();

            var (result, msg) = _runner.RunAsync(testCase).Result;

            var prefix = $"Test {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}: ";
            switch (result)
            {
                case TestResult.Pass:
                    break;

                case TestResult.Fail:
                    Assert.True(false, prefix + msg);
                    break;

                case TestResult.Skip:
                    Skip.If(true, prefix + msg);
                    break;
            }
        } 

        // Since test discovery runs in a separate process, run a dedicated 
        // parse pass as a single unit test to verify all the .txt will parse. 
        // This doesn't actually run any tests. 
        [Fact]
        public void ScanForTxtParseErrors()
        {
            var method = GetType().GetMethod(nameof(InterpreterTestCase));
            var attr = (TxtFileDataAttribute)method.GetCustomAttributes(typeof(TxtFileDataAttribute), false)[0];

            // Verify this runs without throwing an exception.
            var list = attr.GetData(method);

            // And doesn't report back any test failures. 
            foreach (var batch in list)
            {
                var item = (ExpressionTestCase)batch[0];
                Assert.Null(item.FailMessage);
            }
        }

        // Scan the "Not Yet Ready" directory to ensure the tests all parse.
        [Fact]
        public void ScanNotYetReadyForTxtParseErrors()
        {
            var path = Path.Combine("ExpressionTestCases", "NotYetReady");

            // For testing, you can also set path to a full path. 
            // var path = @"D:\dev\pa2\Power-Fx\src\tests\Microsoft.PowerFx.Core.Tests\ExpressionTestCases\NotYetReady";

            path = TxtFileDataAttribute.GetDefaultTestDir(path);

            var runner = new TestRunner();

            // Verify this runs without throwing an exception.
            runner.AddDir(path);

            // Ensure that we actually found tests and not pointed to an empty directory
            Assert.True(runner.Tests.Count > 10);
        }
    }
}
