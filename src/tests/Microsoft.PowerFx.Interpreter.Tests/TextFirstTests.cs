// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TextFirstTests
    {
        [Fact]
        public async void EmptyExpressionTest()
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions(CultureInfo.InvariantCulture) { TextFirst = true };
            var check = engine.Check(string.Empty, parserOptions);

            Assert.Equal(FormulaType.String, check.ReturnType);
            Assert.True(check.IsSuccess);

            var result = (StringValue)await check.GetEvaluator().EvalAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(string.Empty, result.Value);
        }

        [Theory]
        [InlineData("StringInterpolate.txt")]
        [InlineData("TableStringfuncs.txt")]
        [InlineData("string.txt")]
        public void TextFirstRewriterTest(string fileName)
        {            
            var curDir = Path.GetDirectoryName(typeof(TestRunner).Assembly.Location);
            var fileDir = Path.Combine(curDir, "ExpressionTestCases", fileName);

            var engine = new RecalcEngine();

            var testRunner = new TestRunner();
            testRunner.AddFile(TestRunner.ParseSetupString(string.Empty), fileDir);

            foreach (var testCase in testRunner.Tests)
            {
                // Is it that easy?
                var textFirstExpression = $"={testCase.Input}";

                var check = engine.Check(testCase.Input);
                var checkTextFirst = engine.Check(textFirstExpression, options: new ParserOptions() { TextFirst = true });

                Assert.True(check.IsSuccess == checkTextFirst.IsSuccess, $"Failed: {testCase.Input} ===> {textFirstExpression}");

                if (check.IsSuccess)
                {
                    var result = check.GetEvaluator().Eval();
                    var resultTextFirst = checkTextFirst.GetEvaluator().Eval();

                    Assert.Equal(result.ToExpression(), resultTextFirst.ToExpression());
                }
            }
        }
    }
}
