// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Threading;
using Microsoft.PowerFx.Core.Tests.Helpers;
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
    }
}
