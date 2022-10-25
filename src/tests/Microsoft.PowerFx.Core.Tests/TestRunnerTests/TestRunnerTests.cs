// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    // Tests for validating the TestRunner
    public class TestRunnerTests : PowerFxTest
    {
        [Fact]
        public void Test1()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            var tests = runner.Tests.ToArray();
            Assert.Equal(2, tests.Length);

            // Ordered by how we see them in the file. 
            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("expected_result1", tests[0].Expected);
            Assert.Equal("file1.txt:input1", tests[0].GetUniqueId(null));
            Assert.Equal("File1.txt", Path.GetFileName(tests[0].SourceFile), ignoreCase: true);
            Assert.Equal(3, tests[0].SourceLine);

            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].Expected);
            Assert.Equal("file1.txt:input2", tests[1].GetUniqueId(null));
        }

        [Fact]
        public void Test2()
        {
            var runner = new TestRunner();
            AddFile(runner, "File2.txt");

            var tests = runner.Tests.ToArray();
            Assert.Single(tests);

            Assert.Equal("MultiInput\n  secondline", tests[0].Input.Replace("\r", string.Empty));
            Assert.Equal("Result", tests[0].Expected);
        }

        // Override a single file
        [Fact]
        public void TestOverride()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");
            AddFile(runner, "FileOverride.txt");

            var tests = runner.Tests.OrderBy(x => x.Input).ToArray();
            Assert.Equal(2, tests.Length);

            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("override_result1", tests[0].Expected);

            // Other test is unchanged. 
            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].Expected);
        }

        [Theory]
        [InlineData("Bad1.txt")]
        [InlineData("Bad2.txt")]
        [InlineData("Bad3.txt")]
        public void TestBadParse(string file)
        {
            var runner = new TestRunner();

            Assert.Throws<InvalidOperationException>(
                () => AddFile(runner, file));
        }

        // #DISABLE directive to remove an entire file. 
        [Fact]
        public void TestDisable()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            AddFile(runner, "FileDisable.txt");

            Assert.Single(runner.DisabledFiles);
            Assert.Equal("File1.txt", runner.DisabledFiles.First());

            var tests = runner.Tests.ToArray();
            Assert.Single(tests);

            Assert.Equal("input3", tests[0].Input);
            Assert.Equal("result3", tests[0].Expected);
            Assert.Equal("filedisable.txt:input3", tests[0].GetUniqueId(null));
        }

        private static readonly ErrorValue _errorValue = new ErrorValue(IR.IRContext.NotInSource(FormulaType.Number));

        private class MockRunner : BaseRunner
        {
            public Func<string, string, FormulaValue> _hook;

            public Func<string, string, RunResult> _hook2;

            protected override Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName = null)
            {
                if (_hook != null)
                {
                    return Task.FromResult(new RunResult(_hook(expr, setupHandlerName)));
                }

                return Task.FromResult(_hook2(expr, setupHandlerName));
            }
        }

        [Fact]
        public void TestRunnerSuccess()
        {
            var runner = new MockRunner { _hook = (expr, setup) => FormulaValue.New(1) };

            var test = new TestCase
            {
                Expected = "1"
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Pass, result);
        }

        [Fact]
        public void TestRunnerFail()
        {
            var runner = new MockRunner { _hook = (expr, setup) => FormulaValue.New(1) };

            var test = new TestCase
            {
                Expected = "2" // Mismatch!
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Fail, result);
        }

        [Fact]
        public void TestRunnerNumericTolerance()
        {
            var runner = new MockRunner { _hook = (expr, setup) => FormulaValue.New(1.23456789) };

            var test = new TestCase
            {
                Expected = "1.2345654" // difference less than 1e-5
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Pass, result);

            test = new TestCase
            {
                Expected = "1.23455" // difference more than 1e-5
            };
            (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Fail, result);
        }

        [Fact]
        public void TestRunnerSkip()
        {
            var runner = new MockRunner();

            // #SKIP won't even call runner.
            var test = new TestCase
            {
                Expected = "#SKIP"
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Skip, result);
            Assert.NotNull(message);
        }

        // Cases where a test runner marks unsupported behavior. 
        [Fact]
        public void TestRunnerUnsupported()
        {
            var runner = new MockRunner { _hook2 = (expr, setup) => new RunResult { UnsupportedReason = "unsupported" } };
            {
                var test = new TestCase
                {
                    Expected = "1",
                    OverrideFrom = "yes"
                };
                var (result, message) = runner.RunTestCase(test);

                // Fail! Unsupported is only for non-overrides
                Assert.Equal(TestResult.Fail, result);
            }

            {
                var test = new TestCase
                {
                    Expected = "1",
                };
                var (result, message) = runner.RunTestCase(test);

                // Yes, unsupported can skip non-overrides
                Assert.Equal(TestResult.Skip, result);
            }

            {
                // Unsupported can't skip error. We should match the error. 
                var test = new TestCase
                {
                    Expected = "#error",
                };
                var (result, message) = runner.RunTestCase(test);

                Assert.Equal(TestResult.Skip, result);
            }
        }

        [Fact]
        public void TestRunnerCompilerError()
        {
            // Compiler error is a throw from Check()
            var runner = new MockRunner { _hook2 = (expr, setup) => RunResult.FromError("X") };

            var test = new TestCase
            {
                Expected = "Errors: Error X"
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Pass, result);

            // It's a failure if we have the wrong error
            runner._hook2 = (expr, setup) => RunResult.FromError("Y");
            (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Fail, result);

            // Failure if the compiler error is unexpected
            test.Expected = "1";
            (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Fail, result);
        }

        [Fact]
        public void TestSetupHandler()
        {
            const string handlerName = "myhandler";

            var runner = new MockRunner
            {
                _hook = (expr, setup) =>
                {
                    Assert.Equal(setup, handlerName);

                    throw new SetupHandlerNotFoundException();
                }
            };

            var test = new TestCase
            {
                SetupHandlerName = handlerName,
                Expected = "1"
            };

            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Skip, result);
        }

        private static void AddFile(TestRunner runner, string filename)
        {
            var test1 = Path.GetFullPath(filename, TxtFileDataAttribute.GetDefaultTestDir("TestRunnerTests"));
            runner.AddFile(test1);
        }
    }
}
