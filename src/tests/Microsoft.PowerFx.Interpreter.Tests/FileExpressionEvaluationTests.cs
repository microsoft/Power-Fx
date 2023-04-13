// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests : PowerFxTest
    {
        // File expression tests are run TWICE - once with and once without NumberIsFloat
        //
        // Most tests are not sensitive to float vs. decimal and will pass in both modes without modification.
        // If you aren't speficically testing numeric limits, stick to numbers that are less than +/-1E20 which is
        // a safe range for both float and decimal and practically where most makers will work.  
        //
        // For testing large float numbers (for example, 1E100 or 1E300) or high precision decimals
        // (for example, 1.00000000000000000000001), individual files can be excluded from one of the two modes
        // with a directive at the top of the file:
        //   #SKIPFILE: NumberIsFloat          // skips the file if NumberIsFloat is enabled (float mode)
        //   #SKIPFILE: disable:NumberIsFloat  // skips the file if NumberIsFloat is disabled (decimal mode)
        //
        // Skipped files do not show up in the list of skipped tests, tests are skipped before being added in TxtFileData.
        // The intent of SKIPFILE is to be a permanent mode selection for tests that are range/precision sensitive.

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), false)]
        public void InterpreterTestCase(ExpressionTestCase testCase)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over.
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            var runner = new InterpreterRunner() { NumberIsFloat = false };
            var (result, msg) = runner.RunTestCase(testCase);

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

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), true)]
        public void InterpreterTestCase_NumberIsFloat(ExpressionTestCase testCase)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over.
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            var runner = new InterpreterRunner() { NumberIsFloat = true };
            var (result, msg) = runner.RunTestCase(testCase);

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

#if false
        // Helper to run a single .txt 
        [Fact]
        public void RunOne()
        {
            var path = @"D:\dev\pa2\Power-Fx\src\tests\Microsoft.PowerFx.Core.Tests\ExpressionTestCases\OptionSet.txt";
            var line = 41;

            var runner = new InterpreterRunner();
            var testRunner = new TestRunner(runner);

            testRunner.AddFile(path);

            // We can filter to just cases we want 
            if (line > 0)
            {
                testRunner.Tests.RemoveAll(x => x.SourceLine != line);
            }

            var result = testRunner.RunTests();
        }
#endif

        // Run cases in MutationScripts
        // Normal tests have each line as an independent test case. 
        // Whereas these are fed into a repl and each file maintains state. 
        [Theory]
        [InlineData("Simple1.txt")]
        [InlineData("Collect.txt")]
        [InlineData("Clear.txt")]
        [InlineData("ClearCollect.txt")]
        [InlineData("ForAllMutate.txt")]
        public void RunMutationTests(string file)
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "MutationScripts", file);

            //path = @"D:\dev\pa2\Power-Fx\src\tests\Microsoft.PowerFx.Interpreter.Tests\MutationScripts\ForAllMutate.txt";

            var config = new PowerFxConfig();
            config.SymbolTable.EnableMutationFunctions();
            var engine = new RecalcEngine(config);
            var runner = new ReplRunner(engine) { NumberIsFloat = true };

            var testRunner = new TestRunner(runner);

            testRunner.AddFile(numberIsFloat: true, path);

            var result = testRunner.RunTests();

            if (result.Fail > 0)
            {
                Assert.True(false, result.Output);
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
            runner.AddDir(numberIsFloat: false, path);

            // Ensure that we actually found tests and not pointed to an empty directory
            Assert.True(runner.Tests.Count > 10);
        }
    }
}
