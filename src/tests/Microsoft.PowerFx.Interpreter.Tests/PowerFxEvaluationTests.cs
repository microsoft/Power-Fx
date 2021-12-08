// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Tests;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{

    public class ExpressionEvaluationTests
    {
        //[Fact]
        public void RunInterpreterTestCases()
        {
            var runner = new TestRunner(new InterpreterRunner());
            runner.AddDir();
            var result = runner.RunTests();

            // This number should go to 0 over time
            Assert.Equal(36, result.failed);
        }

        // Use this for local testing of a single testcase (uncomment "TestMethod")
        // [Fact]
        public void RunSingleTestCase()
        {
            var runner = new TestRunner(new InterpreterRunner());
            runner.AddFile("Testing.txt");
            var result = runner.RunTests();

            Assert.Equal(0, result.failed);
        }

        internal class InterpreterRunner : BaseRunner
        {
            private RecalcEngine _engine = new RecalcEngine();

            public override Task<FormulaValue> RunAsync(string expr)
            {
                FeatureFlags.StringInterpolation = true;
                var result = _engine.Eval(expr);
                return Task.FromResult(result);
            }
        }
    }
}
