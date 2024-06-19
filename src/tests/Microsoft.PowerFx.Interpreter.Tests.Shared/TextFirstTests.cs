// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TextFirstTests
    {
        [Fact]
        public async Task EmptyExpressionTest()
        {
            var engine = new RecalcEngine();
            var parserOptions = new ParserOptions(CultureInfo.InvariantCulture) { TextFirst = true };
            var check = engine.Check(string.Empty, parserOptions);

            Assert.Equal(FormulaType.String, check.ReturnType);
            Assert.True(check.IsSuccess);

            var result = (StringValue)await check.GetEvaluator().EvalAsync(CancellationToken.None);

            Assert.Equal(string.Empty, result.Value);
        }
    }
}
