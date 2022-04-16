// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;
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

        [Fact]
        public void TestBadParse()
        {
            var runner = new TestRunner();

            Assert.Throws<InvalidOperationException>(
                () => AddFile(runner, "Bad1.txt"));
        }

        [Fact]
        public void TestBad2Parse()
        {
            var runner = new TestRunner();

            Assert.Throws<InvalidOperationException>(
                () => AddFile(runner, "Bad2.txt"));
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

        private static readonly ErrorValue _errorValue = new ErrorValue(IR.IRContext.NotInSource(Public.Types.FormulaType.Number));

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
        public async Task TestRunnerSuccess()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => FormulaValue.New(1)
            };

            var test = new TestCase
            {
                Expected = "1"
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);
        }

        [Fact]
        public async Task TestRunnerFail()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => FormulaValue.New(1)
            };

            var test = new TestCase
            {
                Expected = "2" // Mismatch!
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerNumericTolerance()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => FormulaValue.New(1.23456789)
            };

            var test = new TestCase
            {
                Expected = "1.2345654" // difference less than 1e-5
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);

            test = new TestCase
            {
                Expected = "1.23455" // difference more than 1e-5
            };
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerSkip()
        {
            var runner = new MockRunner();

            // #SKIP won't even call runner.
            var test = new TestCase
            {
                Expected = "#SKIP"
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Skip, result.Item1);
            Assert.NotNull(result.Item2);
        }

        // Cases where a test runner marks unsupported behavior. 
        [Fact]
        public async Task TestRunnerUnsupported()
        {
            var msg = "msg xyz";

            var runner = new MockRunner
            {
                _hook2 = (expr, setup) => new RunResult { UnsupportedReason = "unsupported" }
            };
            {
                var test = new TestCase
                {
                    Expected = "1",
                    OverrideFrom = "yes"
                };
                var result = await runner.RunAsync(test);

                // Fail! Unsupported is only for non-overrides
                Assert.Equal(TestResult.Fail, result.Item1);
            }

            {
                var test = new TestCase
                {
                    Expected = "1",
                };
                var result = await runner.RunAsync(test);

                // Yes, unsupported can skip non-overrides
                Assert.Equal(TestResult.Skip, result.Item1);                
            }

            {
                // Unsupported can't skip error. We should match the error. 
                var test = new TestCase
                {
                    Expected = "#error",
                };
                var result = await runner.RunAsync(test);

                Assert.Equal(TestResult.Skip, result.Item1);
            }
        }

        [Fact]
        public async Task TestRunnerError()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => _errorValue // error
            };

            var test = new TestCase
            {
                Expected = "#ERROR"
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);

            // It's a failure if #error casesucceeds. 
            runner._hook = (expr, setup) => FormulaValue.New(1); // success
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerErrorKindMatching()
        {
            var errorValue = new ErrorValue(
                IR.IRContext.NotInSource(Public.Types.FormulaType.Number),
                new Public.ExpressionError { Kind = Public.ErrorKind.InvalidFunctionUsage });
            var runner = new MockRunner
            {
                _hook = (expr, setup) => errorValue // error
            };

            var test = new TestCase
            {
                Expected = "#Error(Kind=InvalidFunctionUsage)" // validation by enum name
            };
            var result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Pass, result.Item1);

            test = new TestCase
            {
                Expected = "#Error(Kind=16)" // // validation by enum value
            };
            result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Pass, result.Item1);

            test = new TestCase
            {
                Expected = "#Error(Kind=Div0)" // // failure if error kind does not match
            };
            result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Fail, result.Item1);

            test = new TestCase
            {
                Expected = "#Error(Kind=13)" // // failure if numeric error kind does not match
            };
            result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerCompilerError()
        {
            // Compiler error is a throw from Check()
            var runner = new MockRunner
            {
                _hook2 = (expr, setup) => RunResult.FromError("X")
            };

            var test = new TestCase
            {
                Expected = "Errors: Error X"
            };
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);

            // It's a failure if we have the wrong error
            runner._hook2 = (expr, setup) => RunResult.FromError("Y");
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);

            // Failure if the compiler error is unexpected
            test.Expected = "1";
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestSetupHandler()
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
                SetupHandlerName = handlerName
            };
            test.Expected = "1";
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Skip, result.Item1);
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
        public async Task TestErrorOverride()
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
            var result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Pass, result.Item1);

            runner._isError = (value) => false;
            result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Fail, result.Item1);
        }

        // Ensure the #error test fails if the IsError(x) followup call doesn't return true. 
        [Fact]
        public async Task TestErrorOverride2()
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
            var result = await runner.RunAsync(test);
            Assert.Equal(TestResult.Fail, result.Item1);
            Assert.Contains("(IsError() followup call", result.Item2);
        }

        private static void AddFile(TestRunner runner, string filename)
        {
            var test1 = Path.GetFullPath(filename, TxtFileDataAttribute.GetDefaultTestDir("TestRunnerTests"));
            runner.AddFile(test1);
        }
    }
}
