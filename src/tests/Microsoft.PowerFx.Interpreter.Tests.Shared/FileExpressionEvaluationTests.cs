// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests : PowerFxTest
    {
        public readonly ITestOutputHelper Console;

        public FileExpressionEvaluationTests(ITestOutputHelper output)
        {
            Console = output;
        }

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

        // Canvas currently does not support decimal, but since this interpreter does, we can run tests with decimal here.
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "TableSyntaxDoesntWrapRecords,ConsistentOneColumnTableResult,NumberIsFloat,DecimalSupport")]
        [InterpreterTheory]
        public void Canvas_Float(ExpressionTestCase t)
        {
            // current default features in Canvas abc
            var features = new Features()
            {
                TableSyntaxDoesntWrapRecords = true,
                ConsistentOneColumnTableResult = true
            };

            RunExpressionTestCase(t, features, numberIsFloat: true, Console);
        }

        // Canvas currently does not support decimal, but since this interpreter does, we can run tests with decimal here.
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "TableSyntaxDoesntWrapRecords,ConsistentOneColumnTableResult,PowerFxV1CompatibilityRules,NumberIsFloat,DecimalSupport")]
        [InterpreterTheory]
        public void Canvas_Float_PFxV1(ExpressionTestCase t)
        {
            // current default features in Canvas abc
            var features = new Features()
            {
                TableSyntaxDoesntWrapRecords = true,
                ConsistentOneColumnTableResult = true,
                PowerFxV1CompatibilityRules = true,
            };

            RunExpressionTestCase(t, features, numberIsFloat: true, Console);
        }

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "PowerFxV1,disable:NumberIsFloat,DecimalSupport")]
        public void V1_Decimal(ExpressionTestCase t)
        {
            RunExpressionTestCase(t, Features.PowerFxV1, numberIsFloat: false, Console);
        }

        // Although we are using numbers as floats by default, since this interpreter supports decimal, we can run tests with decimal here.        
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner), "PowerFxV1,NumberIsFloat,DecimalSupport")]
        [InterpreterTheory]
        public void V1_Float(ExpressionTestCase t)
        {
            RunExpressionTestCase(t, Features.PowerFxV1, numberIsFloat: true, Console);
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

        private static string _currentNetVersion = null;
        private static readonly object _cnvLock = new object();

        private void RunExpressionTestCase(ExpressionTestCase testCase, Features features, bool numberIsFloat, ITestOutputHelper output)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over.
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            var prefix = $"Test {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}: ";

            // If #DISABLE.NET directive is used, skip the test if the current .NET version is in the list.
            if (!string.IsNullOrEmpty(testCase.DisableDotNet) && ShouldSkipDotNetVersion(testCase, prefix))
            {
                Skip.If(true, prefix + $"Net {_currentNetVersion} is excluded");
                return;
            }

            var runner = new InterpreterRunner() { NumberIsFloat = numberIsFloat, Features = features, Log = (msg) => output.WriteLine(msg) };
            var (result, msg) = runner.RunTestCase(testCase);

            switch (result)
            {
                case TestResult.Pass:
                    break;

                case TestResult.Fail:
                    Assert.Fail(prefix + msg);
                    break;

                case TestResult.Skip:
                    Skip.If(true, prefix + msg);
                    break;
            }
        }

        private static bool ShouldSkipDotNetVersion(ExpressionTestCase testCase, string prefix)
        {
            lock (_cnvLock)
            {
                if (string.IsNullOrEmpty(_currentNetVersion))
                {
                    // Find [assembly: AssemblyTrait(...)] attribute in the test assembly to get the current .NET version.
                    foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(typeof(FileExpressionEvaluationTests).Assembly))
                    {
                        if (cad.AttributeType == typeof(AssemblyTraitAttribute))
                        {
                            _currentNetVersion = cad.ConstructorArguments[1].Value.ToString();
                            break;
                        }
                    }
                }

                if (testCase.DisableDotNet.Split(",").Any(excludedVersion => excludedVersion == _currentNetVersion))
                {
                    return true;
                }
            }

            return false;
        }

#if true
        // Helper to run a single .txt 
        [Fact]
        public void RunOne()
        {
            var path = @"D:\repos\regex-min\src\tests\Microsoft.PowerFx.Core.Tests.Shared\ExpressionTestCases\match_limited.txt";
            var line = 0;

            var runner = new InterpreterRunner();
            var testRunner = new TestRunner(runner);

            testRunner.AddFile(new Dictionary<string, bool>(), path);

            // We can filter to just cases we want, set line above 
            if (line > 0)
            {
                testRunner.Tests.RemoveAll(x => x.SourceLine != line);
            }

            var result = testRunner.RunTests();
            if (result.Fail > 0)
            {
                Assert.True(false, result.Output);
            }
            else
            {
                Console.WriteLine(result.Output);
            }
        }
#endif

        // Run cases in MutationScripts
        //
        // Normal tests have each line as an independent test case. 
        // Whereas these are fed into a repl and each file maintains state.
        // 
        // These tests are run twice, as they are for the non-mutation tests, for both V1 and non-V1 compatibility.
        [Theory]
        [ReplFileSimpleList("MutationScripts")]
        public void RunMutationTests_V1(string file)
        {
            RunMutationTestFile(file, Features.PowerFxV1, "PowerFxV1");
        }

        [Theory]
        [ReplFileSimpleList("MutationScripts")]
        public void RunMutationTests_Canvas(string file)
        {
            var features = new Features()
            {
                TableSyntaxDoesntWrapRecords = true,
                ConsistentOneColumnTableResult = true,
            };

            // disable:PowerFxV1CompatibilityRules will force the tests specifically for those behaviors to be excluded from this run.
            // DecimalSupport allows tests that are written with Float and Decimal functions to operate; it is not itself a feature
            RunMutationTestFile(file, features, "disable:PowerFxV1CompatibilityRules,TableSyntaxDoesntWrapRecords,ConsistentOneColumnTableResult,DecimalSupport");
        }

        private void RunMutationTestFile(string file, Features features, string setup)
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "MutationScripts", file);

            var config = new PowerFxConfig(features) { SymbolTable = UserInfoTestSetup.GetUserInfoSymbolTable() };
            config.SymbolTable.EnableMutationFunctions();
            var engine = new RecalcEngine(config);

            var rc = new RuntimeConfig();
            rc.SetUserInfo(UserInfoTestSetup.UserInfo);

            var runner = new ReplRunner(engine);
            runner._repl.EnableUserObject();
            runner._repl.UserInfo = UserInfoTestSetup.UserInfo.UserInfo;

            // runner._repl.InnerServices = rc.ServiceProvider;

            var testRunner = new TestRunner(runner);

            testRunner.AddFile(TestRunner.ParseSetupString(setup), path);

            if (testRunner.Tests.Count > 0 && testRunner.Tests[0].SetupHandlerName.Contains("MutationFunctionsTestSetup"))
            {
                ExpressionEvaluationTests.MutationFunctionsTestSetup(engine, false);
            }

            var result = testRunner.RunTests();

            if (result.Fail > 0)
            {
                Assert.Fail(result.Output);
            }
            else
            {
                Console.WriteLine(result.Output);
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
            int disableDotNet = 0;

            // And doesn't report back any test failures. 
            foreach (var batch in list)
            {
                var item = (ExpressionTestCase)batch[0];
                Assert.True(item.FailMessage == null, item.FailMessage);

                if (!string.IsNullOrEmpty(item.DisableDotNet))
                {
                    disableDotNet++;
                }
            }

            Console.WriteLine($"Found {list.Count()} tests, {disableDotNet} with DisabledDotNet set.");
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
