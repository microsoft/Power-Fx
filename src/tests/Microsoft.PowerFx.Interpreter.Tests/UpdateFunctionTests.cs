// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Functions.Mutation;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UpdateFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData(typeof(UpdateFunction))]
        public async Task CheckArgsTestAsync(Type type)
        {
            var expressionError = new ExpressionError()
            {
                Kind = ErrorKind.ReadOnlyValue,
                Severity = ErrorSeverity.Critical,
                Message = "Something went wrong"
            };

            FormulaValue[] args = new[]
            {
                FormulaValue.NewError(expressionError)
            };

            var function = Activator.CreateInstance(type) as IAsyncTexlFunction;
            var result = await function.InvokeAsync(args, CancellationToken.None).ConfigureAwait(false);

            Assert.IsType<ErrorValue>(result);
        }
    }
}
