// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core.Tests
{
    // Base class for running a lightweght test. 
    public abstract class BaseRunner
    {
        /// <summary>
        /// Maximum time to run test - this catches potential hangs in the engine. 
        /// Any test should easily run in under 1s. 
        /// </summary>
        public static TimeSpan Timeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Runs a PowerFx test case, with optional setup.
        /// </summary>
        /// <param name="expr">PowerFx expression.</param>
        /// <param name="setupHandlerName">Optional name of a setup handler to run. Throws SetupHandlerNotImplemented if not found.</param>
        /// <returns>Result of evaluating Expr.</returns>
        protected abstract Task<FormulaValue> RunAsyncInternal(string expr, string setupHandlerName = null);

        private static readonly Regex RuntimeErrorExpectedResultRegex = new Regex(@"\#error(?:\(Kind=(?<errorKind>[^\)]+)\))?", RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns (Pass,Fail,Skip) and a status message.
        /// </summary>
        /// <param name="test">test case to run.</param>
        /// <returns>status from running.</returns>
        public async Task<(TestResult, string)> RunAsync(TestCase testCase)
        {
            var result = TestResult.Fail;
            string message = null;

            var t = new Thread(() =>
            {
                (result, message) = RunAsync2(testCase).Result;
            });
            t.Start();
            bool success;
            while (true) 
            {
                success = t.Join(Timeout);
                if (!success && Debugger.IsAttached)
                {
                    // Aid in debugging.
                    Debugger.Log(0, null, $"Test case {testCase} running...\r\n");
                    
                    // Debugger.Break();
                    continue;
                }

                break;
            }            

            if (success)
            {
                return (result, message);
            }
            else
            {
                // Timeout!!!
                return (TestResult.Fail, $"Timeout after {Timeout}");
            }
        }

        // If we override Error values, then for a test case:
        //   >> X
        //   #error
        // also check that:
        //   >> IsError(x)
        //   true
        private async Task<(TestResult, string)> RunErrorCaseAsync(TestCase testCase)
        {
            var case2 = new TestCase
            {
                SetupHandlerName = testCase.SetupHandlerName,
                SourceLine = testCase.SourceLine,
                SourceFile = testCase.SourceFile,
                Input = $"IsError({testCase.Input})",
                Expected = "true"
            };

            var (result, msg) = await RunAsync2(case2);
            if (result == TestResult.Fail)
            {
                msg += " (IsError() followup call)";
            }

            return (result, msg);
        }

        private async Task<(TestResult, string)> RunAsync2(TestCase testCase)
        {
            string actualStr;
            FormulaValue result = null;

            var expected = testCase.Expected;
            var expectedSkip = string.Equals(expected, "#skip", StringComparison.OrdinalIgnoreCase);
            if (expectedSkip)
            {
                // Skipped test should fail when run. 
                return (TestResult.Skip, $"was skipped by request");
            }

            try
            {
                try
                {
                    result = await RunAsyncInternal(testCase.Input, testCase.SetupHandlerName);
                }
                catch (SetupHandlerNotFoundException)
                {
                    return (TestResult.Skip, $"was skipped due to missing setup handler {testCase.SetupHandlerName}");
                }

                actualStr = TestRunner.TestToString(result);
            }
            catch (Exception e)
            {
                // Expected will contain the full error message, like:
                //    Errors: Error 15-16: Incompatible types for comparison. These types can't be compared: UntypedObject, UntypedObject.
                var expectedCompilerError = expected.StartsWith("Errors: Error"); // $$$ Match error message. 
                if (expectedCompilerError)
                {
                    actualStr = e.Message.Replace("\r\n", "|").Replace("\n", "|");

                    if (actualStr.Contains(expected))
                    {
                        // Compiler errors result in exceptions
                        return (TestResult.Pass, null);
                    }
                    else
                    {
                        return (TestResult.Fail, $"Failed, but wrong error message: {e.Message}");
                    }
                }
                else
                {
                    return (TestResult.Fail, $"Threw exception: {e.Message}");
                }
            }

            if (result == null)
            {
                return (TestResult.Fail, "did not return a value");
            }

            var expectedRuntimeErrorMatch = RuntimeErrorExpectedResultRegex.Match(expected);

            if (expectedRuntimeErrorMatch.Success)
            {
                var expectedErrorKindGroup = expectedRuntimeErrorMatch.Groups["errorKind"];
                var expectedErrorKind = expectedErrorKindGroup.Success ? expectedErrorKindGroup.Value : null;
                if (result is ErrorValue errorResult)
                {
                    if (expectedErrorKind == null)
                    {
                        // If no kind is part of the expected value, just return a pass if the result is an error
                        return (TestResult.Pass, null);
                    }

                    var actualErrorKind = errorResult.Errors.First().Kind;
                    if (int.TryParse(expectedErrorKind, out var numericErrorKind))
                    {
                        // Error given as the internal value
                        if (numericErrorKind == (int)actualErrorKind)
                        {
                            return (TestResult.Pass, null);
                        }
                        else
                        {
                            return (TestResult.Fail, $"Received an error, but expected kind={expectedErrorKind} and received {actualErrorKind} ({(int)actualErrorKind})");
                        }
                    }
                    else if (Enum.TryParse<ErrorKind>(expectedErrorKind, out var errorKind))
                    {
                        // Error given as the enum name
                        if (errorKind == actualErrorKind)
                        {
                            return (TestResult.Pass, null);
                        }
                        else
                        {
                            return (TestResult.Fail, $"Received an error, but expected kind={errorKind} and received {actualErrorKind}");
                        }
                    }
                    else
                    {
                        return (TestResult.Fail, $"Invalid expected error kind: {expectedErrorKind}");
                    }
                }
                else if (IsError(result))
                {
                    // If they override IsError, then do additional checks. 
                    return await RunErrorCaseAsync(testCase);
                }

                // If the actual result is not an error, we'll fail with a mismatch below
            }

            if (string.Equals(expected, actualStr, StringComparison.Ordinal))
            {
                return (TestResult.Pass, null);
            }

            if (result is NumberValue numericResult && double.TryParse(expected, out var expectedNumeric))
            {
                // Allow for a 1e-5 difference
                var diff = Math.Abs(numericResult.Value - expectedNumeric);
                if (diff < 1e-5)
                {
                    return (TestResult.Pass, null);
                }

                // diff in large numbers, Precision diff is small, but exponent can be large. 
                // 5.5e186 vs 5.6e186
                if (expectedNumeric != 0)
                {
                    if (Math.Abs((numericResult.Value - expectedNumeric) / expectedNumeric) < 1e-14)
                    {
                        return (TestResult.Pass, null);
                    }
                }
            }

            return (TestResult.Fail, $"Expected: {expected}. actual: {actualStr}");
        }

        // Get the friendly name of the harness. 
        public virtual string GetName()
        {
            return GetType().Name;
        }

        // Some hosts don't have a way to natively represent an error, and so top level errors values
        // are converted to something like blank. 
        public virtual bool IsError(FormulaValue value)
        {
            return value is ErrorValue;
        }
    }

    public enum TestResult
    {
        Pass,
        Skip,

        // Failure could be:
        // - wrong result (including a runtime error)
        // - compiler error
        // - other
        Fail
    }
}
