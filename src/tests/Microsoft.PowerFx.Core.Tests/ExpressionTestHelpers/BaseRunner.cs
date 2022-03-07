// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            var success = t.Join(Timeout);

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

            var expectedRuntimeError = string.Equals(expected, "#error", StringComparison.OrdinalIgnoreCase);
            
            if (expectedRuntimeError)
            {
                var isError = IsError(result);
                if (isError)
                {
                    return (TestResult.Pass, null);
                }

                // we'll fail with a mismatch below
            }
                        
            if (string.Equals(expected, actualStr, StringComparison.Ordinal))
            {
                return (TestResult.Pass, null);
            }

            return (TestResult.Fail, $"Expected: {expected}. actual: {actualStr}");            
        }

        // Get the friendly name of the harness. 
        public virtual string GetName()
        {
            return GetType().Name;
        }

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
