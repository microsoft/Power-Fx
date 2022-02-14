// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests
    {
        private InterpreterRunner _runner;

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", nameof(InterpreterRunner))]
        public void InterpreterTestCase(ExpressionTestCase testCase)
        {
            _runner = new InterpreterRunner();
            var engineName = _runner.GetName();

            string actualStr;
            FormulaValue result = null;
            var exceptionThrown = false;
            try
            {
                if (testCase.SetupHandlerName != null)
                {
                    try
                    {
                        result = _runner.RunWithSetup(testCase.Input, testCase.SetupHandlerName).Result;
                    }
                    catch (NotSupportedException ex) when (ex.Message.Contains("Setup Handler"))
                    {
                        Skip.If(true, $"Test {testCase.SourceFile}:{testCase.SourceLine} was skipped due to missing setup handler {testCase.SetupHandlerName}");
                    }
                }
                else 
                {
                    result = _runner.RunAsync(testCase.Input).Result;
                }

                actualStr = TestRunner.TestToString(result);
            }
            catch (Exception e)
            {
                actualStr = e.Message.Replace("\r\n", "|");
                exceptionThrown = true;
            }

            if ((exceptionThrown && testCase.GetExpected(nameof(InterpreterRunner)) == "Compile Error") || (result != null && testCase.GetExpected(nameof(InterpreterRunner)) == "#Error" && _runner.IsError(result)))
            {
                // Pass as test is expected to return an error
                return;
            }

            if (testCase.GetExpected(nameof(InterpreterRunner)) == "#Skip")
            {
                var goodResult = testCase.GetExpected("-");
                Assert.False(goodResult == actualStr || (goodResult == "#Error" && _runner.IsError(result)), "Test marked to skip returned correct result");

                // Since test is marked to skip and it didn't return a result that matched the baseline
                // expected result then we can marked it skipped here
                Skip.If(true, $"Test {testCase.SourceFile}:{testCase.SourceLine} was skipped by request");
            }

            Assert.Equal(testCase.GetExpected(nameof(InterpreterRunner)), actualStr);
        }
    }
}
