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

        [Fact]
        public void TestList()
        {
            var mock = new MockErrorRunner
            {
                _hook = (expr, setup) => FormulaValue.New(int.Parse(expr))
            };

            // Edit test directly 
            var runner = new TestRunner(mock);
            runner.Tests.Add(new TestCase
            {
                Input = "1",
                Expected = "1"
            });
            runner.Tests.Add(new TestCase
            {
                Input = "2",
                Expected = "1"
            });
            runner.Tests.Add(new TestCase
            {
                Input = "2",
                Expected = "2"
            });

            var summary = runner.RunTests();
            Assert.Equal(3, summary.Total);
            Assert.Equal(1, summary.Fail);
            Assert.Equal(2, summary.Pass);
        }

        private const string LongForm5e186 = "5579910311786366000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        private const string ShortForm5e186 = "5.579910311786367E+186";

        [Theory]
        [InlineData(LongForm5e186, ShortForm5e186, true)]
        [InlineData("1.57", "1.57", true)] // Same
        [InlineData("1.570", "1.5700", true)] // trailing 0s
        [InlineData("1.57", "1.56", false)]
        [InlineData("5.5e186", "5.6e186", false)]
        [InlineData("-" + LongForm5e186, LongForm5e186, false)] // large positive vs. negative
        public void TestLargeNumberPass(string a, string b, bool pass)
        {
            var mock = new MockErrorRunner
            {
                _hook = (expr, setup) => FormulaValue.New(double.Parse(expr))
            };

            // do both orders. 
            var runner = new TestRunner(mock);
            runner.Tests.Add(new TestCase
            {
                Input = a,
                Expected = b
            });
            runner.Tests.Add(new TestCase
            {
                Input = b,
                Expected = a
            });

            var summary = runner.RunTests();

            if (pass)
            {
                Assert.Equal(0, summary.Fail);
                Assert.Equal(2, summary.Pass);
            } 
            else
            {
                Assert.Equal(2, summary.Fail);
                Assert.Equal(0, summary.Pass);
            }
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
        public void TestRunnerError()
        {
            var runner = new MockRunner { _hook = (expr, setup) => _errorValue /* error */ };

            var test = new TestCase
            {
                Expected = "#ERROR"
            };
            var (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Pass, result);

            // It's a failure if #error casesucceeds. 
            runner._hook = (expr, setup) => FormulaValue.New(1); // success
            (result, message) = runner.RunTestCase(test);

            Assert.Equal(TestResult.Fail, result);
        }

        [Fact]
        public void TestRunnerErrorKindMatching()
        {
            var errorValue = new ErrorValue(IR.IRContext.NotInSource(FormulaType.Number), new ExpressionError { Kind = ErrorKind.InvalidFunctionUsage });
            
            var runner = new MockRunner { _hook = (expr, setup) => errorValue /* error */ };

            var test = new TestCase
            {
                Expected = "#Error(Kind=InvalidFunctionUsage)" // validation by enum name
            };
            var (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Pass, result);

            test = new TestCase
            {
                Expected = "#Error(Kind=16)" // // validation by enum value
            };
            (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Pass, result);

            test = new TestCase
            {
                Expected = "#Error(Kind=Div0)" // // failure if error kind does not match
            };
            (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Fail, result);

            test = new TestCase
            {
                Expected = "#Error(Kind=13)" // // failure if numeric error kind does not match
            };
            (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Fail, result);
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

        // Override IsError
        private class MockErrorRunner : MockRunner
        {
            protected override Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName = null)
            {
                return Task.FromResult(new RunResult(_hook(expr, setupHandlerName)));
            }

            public Func<FormulaValue, bool> _isError;

            public override bool IsError(FormulaValue value)
            {
                if (_isError != null)
                {
                    return _isError(value);
                }

                return base.IsError(value);
            }
        }

        [Fact]
        public void TestErrorOverride()
        {
            // Test override BaseRunner.IsError
            var runner = new MockErrorRunner
            {
                _hook = (expr, setup) =>
                    expr switch
                    {
                        "1" => FormulaValue.New(1),
                        "IsError(1)" => FormulaValue.New(true),
                        _ => throw new InvalidOperationException()
                    },
                _isError = (value) => value is NumberValue
            };

            var test = new TestCase
            {
                Input = "1",
                Expected = "#error"
            };

            // On #error for x, test runner  will also call IsError(x)
            var (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Pass, result);

            runner._isError = (value) => false;
            (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Fail, result);
        }

        // Ensure the #error test fails if the IsError(x) followup call doesn't return true. 
        [Fact]
        public void TestErrorOverride2()
        {
            // Test override BaseRunner.IsError
            var runner = new MockErrorRunner
            {
                _hook = (expr, setup) =>
                    expr switch
                    {
                        "1" => FormulaValue.New(1),
                        "IsError(1)" => FormulaValue.New(false), // expects true, should cause failure
                        _ => throw new InvalidOperationException()
                    },
                _isError = (value) => value is NumberValue
            };

            var test = new TestCase
            {
                Input = "1",
                Expected = "#error"
            };

            // On #error for x, test runner  will also call IsError(x)
            var (result, message) = runner.RunTestCase(test);
            Assert.Equal(TestResult.Fail, result);
            Assert.Contains("(IsError() followup call", message);
        }

        private static void AddFile(TestRunner runner, string filename)
        {
            var test1 = Path.GetFullPath(filename, TxtFileDataAttribute.GetDefaultTestDir("TestRunnerTests"));
            runner.AddFile(test1);
        }
    }
}
