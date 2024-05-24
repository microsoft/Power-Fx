// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Result of a call to <see cref="BaseRunner.RunAsyncInternal(string, string)"/>.
    /// - Compilation error - set Errors
    /// - Runtime error - set Value
    ///
    /// If the eval used unsupported behavior, set UnsupportedReason to a message.
    /// This may cause th etest to get skipped rather than fail. 
    /// </summary>
    public class RunResult
    {
        // Test case had a Compilation error.
        // Null if none. 
        public ExpressionError[] Errors;

        // Test case ran and returned a result. 
        public FormulaValue Value;

        public FormulaValue OriginalValue;

        // Test may have run and failed (so Value is set) due to
        // known unsupported behavior in an engine. Let engine mark that this is a known case.
        //
        // This will often mean skipping the test, but may still be a failure if:
        // - the test is already from an override (so it should have marked #skip)
        // - the test already expected to fail (so we should have gotten the failure)
        //
        // This allows tests to avoid lots of overrides for known behavior that isn't implemented.
        public string UnsupportedReason;

        public RunResult()
        {
        }

        public RunResult(FormulaValue value, FormulaValue originalValue = null)
        {
            Value = value;
            OriginalValue = originalValue;
        }

        public RunResult(CheckResult result)
        {
            if (!result.IsSuccess)
            {
                Errors = result.Errors.ToArray();
            }
        }

        public static RunResult FromError(string message)
        {
            return new RunResult()
            {
                Errors = new ExpressionError[]
                {
                    new ExpressionError
                    {
                         Message = message,
                         Severity = ErrorSeverity.Severe
                    }
                }
            };
        }
    }

    // Base class for running a lightweght test. 
    public abstract class BaseRunner
    {
        /// <summary>
        /// Maximum time to run test - this catches potential hangs in the engine. 
        /// Any test should easily run in under 1s. 
        /// </summary>
        public static TimeSpan Timeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Should the NumberIsFloat parser flag be in effect.
        /// </summary>
        public bool NumberIsFloat { get; set; }

        /// <summary>
        /// What base Features should be enabled, before adding file level #SETUP and #DISABLE.
        /// </summary>
        public Features Features = Features.None;

        /// <summary>
        /// Logger action.
        /// </summary>
        public Action<string> Log = null;

        /// <summary>
        /// Runs a PowerFx test case, with optional setup.
        /// </summary>
        /// <param name="expr">PowerFx expression.</param>
        /// <param name="setupHandlerName">Optional name of a setup handler to run. Throws SetupHandlerNotImplemented if not found.</param>        
        /// <returns>Result of evaluating Expr.</returns>
        protected abstract Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName = null);

        /// <summary>
        /// Returns (Pass,Fail,Skip) and a status message.
        /// </summary>
        /// <param name="test">test case to run.</param>
        /// <returns>status from running.</returns>
        public (TestResult result, string message) RunTestCase(TestCase testCase)
        {
            var t = Task.Factory.StartNew(
                () =>
                {
                    var t = RunAsync2(testCase);
                    t.ConfigureAwait(false);

                    return t.Result;
                },
                new CancellationToken(),
                TaskCreationOptions.None,
                TaskScheduler.Default);

            while (true)
            {
                Task.WaitAny(t, Task.Delay(Timeout));

                if (t.IsCompletedSuccessfully)
                {
                    return t.Result;
                }
                else
                {
                    // Timeout!!!
                    if (Debugger.IsAttached && !t.IsCompleted)
                    {
                        // Aid in debugging.
                        Debugger.Log(0, null, $"Test case {testCase} running...\r\n");

                        // Debugger.Break();
                        continue;
                    }

                    return (TestResult.Fail, $"Timeout after {Timeout}");
                }
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

            var (result, msg) = await RunAsync2(case2).ConfigureAwait(false);
            if (result == TestResult.Fail)
            {
                msg += " (IsError() followup call)";
            }

            return (result, msg);
        }

        private async Task<(TestResult, string)> RunAsync2(TestCase testCase)
        {
            RunResult runResult = null;
            FormulaValue result = null;
            FormulaValue originalResult = null;

            var expected = testCase.Expected;

            var expectedSkip = Regex.Match(expected, "^\\s*\\#skip", RegexOptions.IgnoreCase).Success;
            if (expectedSkip)
            {
                // Skipped test should fail when run. 
                return (TestResult.Skip, $"was skipped by request");
            }

            try
            {
                runResult = await RunAsyncInternal(testCase.Input, testCase.SetupHandlerName).ConfigureAwait(false);
                result = runResult.Value;
                originalResult = runResult.OriginalValue;

                // Unsupported is just for ignoring large groups of inherited tests. 
                // If it's an override, then the override should specify the exact error.
                if (!testCase.IsOverride && runResult.UnsupportedReason != null)
                {
                    return (TestResult.Skip, "Unsupported in this engine: " + runResult.UnsupportedReason);
                }

                // Check for a compile-time error.
                if (runResult.Errors != null && runResult.Errors.Length > 0)
                {
                    // Matching text to CheckResult.ThrowOnErrors()
                    // Expected will contain the full error message, like:
                    //    Errors: Error 15-16: Incompatible types for comparison. These types can't be compared: UntypedObject, UntypedObject.
                    var expectedCompilerError = expected.StartsWith("Errors: Error") || expected.StartsWith("Errors: Warning"); // $$$ Match error message. 
                    if (expectedCompilerError)
                    {
                        string[] expectedStrArr = expected.Replace("Errors: ", string.Empty).Split("|");
                        string[] actualStrArr = runResult.Errors.Select(err => err.ToString()).ToArray();
                        bool isValid = true;

                        // Try both unaltered comparison and by replacing Decimal with Number for errors,
                        // for tests that are run with and without NumberIsFloat set.
                        foreach (var exp in expectedStrArr)
                        {
                            if (!actualStrArr.Contains(exp) && 
                                !(NumberIsFloat && actualStrArr.Contains(Regex.Replace(exp, "(?<!Number,)(\\s|'|\\()Decimal(\\s|'|,|\\.|\\))", "$1Number$2"))) &&
                                !(NumberIsFloat && actualStrArr.Contains(Regex.Replace(exp, " Decimal, Number, ", " Number, Decimal, "))))
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (isValid)
                        {
                            // Compiler errors result in exceptions
                            return (TestResult.Pass, null);
                        }
                        else
                        {
                            return (TestResult.Fail, $"Failed, but wrong error message: {$"Errors: " + string.Join("\r\n", actualStrArr)}");
                        }
                    }
                }
            }
            catch (SetupHandlerNotFoundException shnfe)
            {
                return (TestResult.Skip, $"was skipped due to error with setup handler {testCase.SetupHandlerName} - {shnfe.InnerException?.Message}");
            }
            catch (Exception e)
            {
                return (TestResult.Fail, $"Threw exception: {e.Message}, {e.StackTrace}");
            }

            if (result is not ErrorValue && expected.StartsWith("Error") && IsError(result) && testCase.Input != null)
            {
                // If they override IsError, then do additional checks. 
                return await RunErrorCaseAsync(testCase).ConfigureAwait(false);
            }

            // If the actual result is not an error, we'll fail with a mismatch below
            if (result == null)
            {
                var msg = "Did not return a value";

                if (runResult.Errors != null && runResult.Errors.Any())
                {
                    msg += string.Join(string.Empty, runResult.Errors.Select(err => "\r\n" + err));
                }

                return (TestResult.Fail, msg);
            }
            else
            {
                var sb = new StringBuilder();

                var settings = new FormulaValueSerializerSettings()
                {
                    UseCompactRepresentation = true,
                };

                // Serializer will produce a human-friedly representation of the value
                result.ToExpression(sb, settings);

                var actualStr = sb.ToString();

                if (string.Equals(expected, actualStr, StringComparison.Ordinal))
                {
                    return (TestResult.Pass, null);
                }

                // Check to see if it is a number, this test covers decimal too as decimal fits in double
                if (double.TryParse(expected, out var expectedFloat))
                {
                    // fuzzy compare of floating point numbers (which didn't strict match above)
                    if (originalResult == null || originalResult is NumberValue)
                    {
                        // if the expected result is high precision, and a float was returned, there is no way they can match
                        // this is important for tests that check for a decimal return, for which the value would be rounded to a float and would pass
                        if (decimal.TryParse(expected, out var expectdDecimal) &&
                            (decimal.Round(expectdDecimal, 17) != expectdDecimal))
                        {
                            return (TestResult.Fail, $"\r\n  Float result {expectedFloat} can't match high precision Decimal expected {expected}");
                        }

                        if (result is NumberValue numericResult)
                        {
                            if (NumberCompare(numericResult.Value, expectedFloat))
                            {
                                return (TestResult.Pass, null);
                            }
                        }
                        else if (result is DecimalValue decimalResult)
                        {
                            if (NumberCompare((double)decimalResult.Value, expectedFloat))
                            {
                                return (TestResult.Pass, null);
                            }
                        }
                    }

                    // strict compare binary decimal values after being parsed (for printing differences)
                    else if (originalResult is DecimalValue dec && decimal.Parse(expected, System.Globalization.NumberStyles.Float) == dec.Value)
                    {
                        return (TestResult.Pass, null);
                    }
                }

                decimal.TryParse(expected, out var ee);

                // strict compare of decimal numbers, with fuzzy compare of floating equivalent (which didn't strict match above)
                if ((originalResult is DecimalValue origDecimal) && (result is NumberValue resNumber) && double.TryParse(expected, out var expectedNumber))
                {
                    var sb2 = new StringBuilder();
                    originalResult.ToExpression(sb2, settings);
                    var actualDecimal = sb2.ToString();

                    if (string.Equals(expected, actualDecimal, StringComparison.Ordinal) &&
                        NumberCompare(resNumber.Value, expectedNumber))
                    {
                        return (TestResult.Pass, null);
                    }
                }

                return (TestResult.Fail, $"\r\n  Expected: {expected}\r\n  Actual  : {actualStr}");
            }
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

        // Derived harness may need to override if they have a different precision level. 
        public virtual bool NumberCompare(double a, double b)
        {
            // Allow for a 1e-5 difference
            var diff = Math.Abs(a - b);
            if (diff < 1e-5)
            {
                return true;
            }

            // diff in large numbers, Precision diff is small, but exponent can be large. 
            // 5.5e186 vs 5.6e186
            if (b != 0)
            {
                if (Math.Abs(diff / b) < 1e-14)
                {
                    return true;
                }
            }

            return false;
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
