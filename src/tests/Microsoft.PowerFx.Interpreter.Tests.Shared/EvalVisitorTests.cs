// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // This file should eventually get 100% code-coverage on EvalVistor.
    // Exercise via RecalcEngine 
    public class EvalVistitorTests
    {
        [Theory]
        [InlineData(
            "Join([10,20],[1,2], LeftRecord.Value = RightRecord.Value*10, JoinType.Left, RightRecord.Value As R2)", "Table({R2:1,Value:10},{R2:2,Value:20})")]
        [InlineData(
            "Filter([10,30,20], ThisRecord.Value > 15)",
            "Table({Value:30},{Value:20})")]
        public void Dispatch(string expr, string expected)
        {
            string answer = Run(expr);

            Assert.Equal(expected, answer);
        }

        private static readonly RecalcEngine _engine = NewEngine();

        private static RecalcEngine NewEngine()
        {
            var config = new PowerFxConfig();
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableJoinFunction();
#pragma warning restore CS0618 // Type or member is obsolete
            var engine = new RecalcEngine(config);

            return engine;
        }

        private static string Run(string expr)
        {
            var result = _engine.Eval(expr);

            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings { UseCompactRepresentation = true });

            var answer = sb.ToString();
            return answer;
        }
    }    
}
