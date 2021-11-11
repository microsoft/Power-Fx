// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.xUnitExtensions;
using Xunit;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests
    {
        private InterpreterRunner _runner;

        [InterpreterTheory()]
        [TxtFileData("ExpressionTestCases", nameof(InterpreterRunner))]
        public void InterpreterTestCase(ExpressionTestCase testCase)
        {
            _runner = new InterpreterRunner();
            var engineName = _runner.GetName();

            string actualStr;
            FormulaValue result = null;
            try
            {
                result = _runner.RunAsync(testCase.Input).Result;
                actualStr = TestToString(result);
            }
            catch (Exception e)
            {
                actualStr = e.Message.Replace("\r\n", "|");
            }

            if (result != null && testCase.GetExpected(nameof(InterpreterRunner)) == "#Error" && _runner.IsError(result))
            {
                // Pass as test is expected to return an error
                return;
            }

            if (testCase.GetExpected(nameof(InterpreterRunner)) == "#Skip")
            {
                var goodResult = testCase.GetExpected("-");
                Assert.False(goodResult == actualStr || goodResult == "#Error" && _runner.IsError(result), "Test marked to skip returned correct result");

                // Since test is marked to skip and it didn't return a result that matched the baseline
                // expected result then we can marked it skipped here
                Skip.If(true, $"Test {testCase.SourceFile}:{testCase.SourceLine} was skipped by request");
            }

            Assert.Equal(testCase.GetExpected(nameof(InterpreterRunner)), actualStr);
        }

        internal string TestToString(FormulaValue result)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                TestToString(result, sb);
            }
            catch (Exception e)
            {
                // This will cause a diff and test failure below. 
                sb.Append($"<exception writing result: {e.Message}>");
            }

            return sb.ToString();
        }

        internal void TestToString(FormulaValue result, StringBuilder sb)
        {
            if (result is NumberValue n)
            {
                sb.Append(n.Value);
            }
            else if (result is DateValue d)
            {
                sb.Append(d.Value.ToString("d"));
            }
            else if (result is BooleanValue b)
            {
                sb.Append(b.Value ? "true" : "false");
            }
            else if (result is StringValue s)
            {
                // $$$ proper escaping?
                sb.Append('"' + s.Value + '"');
            }
            else if (result is TableValue t)
            {
                sb.Append('[');

                string dil = "";
                foreach (var row in t.Rows)
                {
                    sb.Append(dil);

                    if (row.IsValue)
                    {
                        var tableType = (TableType)t.Type;
                        if (t.IsColumn && tableType.GetNames().First().Name == "Value")
                        {
                            var val = row.Value.Fields.First().Value;
                            TestToString(val, sb);
                        }
                        else
                        {
                            TestToString(row.Value, sb);
                        }
                    }
                    else
                    {
                        TestToString(row.ToFormulaValue(), sb);
                    }

                    dil = ",";
                }
                sb.Append(']');
            }
            else if (result is RecordValue r)
            {
                var fields = r.Fields.ToArray();
                Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

                sb.Append('{');
                string dil = "";

                foreach (var field in fields)
                {
                    sb.Append(dil);
                    sb.Append(field.Name);
                    sb.Append(':');
                    TestToString(field.Value, sb);

                    dil = ",";
                }
                sb.Append('}');
            }
            else if (result is BlankValue)
            {
                sb.Append("Blank()");
            }
            else if (result is ErrorValue)
            {
                sb.Append(result);
            }
            else
            {
                throw new InvalidOperationException($"unsupported value type: {result.GetType().Name}");
            }
        }

    }

}
