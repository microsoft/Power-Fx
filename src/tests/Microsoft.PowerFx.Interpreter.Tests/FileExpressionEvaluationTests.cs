// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests : PowerFxTest
    {
        // File expression tests are run multiple times for the different ways a host can use Power Fx.
        // 
        // 1. Features.PowerFxV1 without NumberIsFloat - the main way that most hosts will use Power Fx.
        // 2. Feautres.PowerFxV1 with NumberIsFloat - for hosts that wish to use floating point instead of Decimal.
        // 3. Default Canvas features with NumberIsFloat - the current default for Canvas apps.  Canvas
        //    has an internal relationship with the compiler that allows it to run with a different mix of features.
        // 4. No features with NumberIsFloat (occasional) - important for back compat convertes in Canvas as the
        //    back compat converters depend on the feature mix being the same as when the original app was serialized.
        //
        // See the README.md in the ExpressionTestCases directory for more details.

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "TableSyntaxDoesntWrapRecords,ConsistentOneColumnTableResult,NumberIsFloat")]
        public void Canvas_Float(ExpressionTestCase testCase)
        {
            // current default features in Canvas abc
            var features = new Features()
            {
                TableSyntaxDoesntWrapRecords = true,
                ConsistentOneColumnTableResult = true
            };

            RunExpressionTestCase(testCase, features, numberIsFloat: true);
        }

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "PowerFxV1,disable:NumberIsFloat")]
        public void V1_Decimal(ExpressionTestCase testCase)
        {
            RunExpressionTestCase(testCase, Features.PowerFxV1, numberIsFloat: false);
        }

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "PowerFxV1,NumberIsFloat")]
        public void V1_Float(ExpressionTestCase testCase)
        {
            RunExpressionTestCase(testCase, Features.PowerFxV1, numberIsFloat: true);
        }

#if false
        // This does not need to be run every time, but should be run periodically.
        // Keeping this clean ensures that back compat converters in Canvas continue to function properly.
        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "NumberIsFloat")]
        public void None_Float(ExpressionTestCase testCase)
        {
            RunExpressionTestCase(testCase, Features.None, numberIsFloat: true);
        }
#endif

        private void RunExpressionTestCase(ExpressionTestCase testCase, Features features, bool numberIsFloat)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over.
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            var runner = new InterpreterRunner() { NumberIsFloat = numberIsFloat, Features = features };
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
        [InlineData("Patch.txt")]
        [InlineData("Clear.txt")]
        [InlineData("ClearCollect.txt")]
        [InlineData("ForAllMutate.txt")]
        [InlineData("CopyOnWrite.txt")]
        public void RunMutationTests(string file)
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "MutationScripts", file);

            //path = @"D:\dev\pa2\Power-Fx\src\tests\Microsoft.PowerFx.Interpreter.Tests\MutationScripts\ForAllMutate.txt";

            var config = new PowerFxConfig();
            config.SymbolTable.EnableMutationFunctions();
            var engine = new RecalcEngine(config);
            var runner = new ReplRunner(engine) { NumberIsFloat = true };

            var testRunner = new TestRunner(runner);

            testRunner.AddFile(TestRunner.ParseSetupString("NumberIsFloat"), path);

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
            var method = GetType().GetMethod(nameof(Canvas_Float));
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
            runner.AddDir(new Dictionary<string, bool>(), path);

            // Ensure that we actually found tests and not pointed to an empty directory
            Assert.True(runner.Tests.Count > 10);
        }
    }
}
