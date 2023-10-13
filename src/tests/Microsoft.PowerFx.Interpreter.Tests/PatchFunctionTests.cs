// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PatchFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData(typeof(PatchFunctionImpl))]
        public async Task CheckArgsTestAsync(Type type)
        {
            var expressionError = new ExpressionError()
            {
                Kind = ErrorKind.ReadOnlyValue,
                Severity = ErrorSeverity.Critical,
                Message = "Something went wrong"
            };

            FormulaValue[] args = new[] { FormulaValue.NewError(expressionError) };

            IFunctionImplementation function = Activator.CreateInstance(type) as IFunctionImplementation;
            BasicServiceProvider serviceProvider = new BasicServiceProvider();
            serviceProvider.AddService(new FunctionExecutionContext(args));
            FormulaValue result = await function.InvokeAsync(serviceProvider, CancellationToken.None).ConfigureAwait(false);

            Assert.IsType<ErrorValue>(result);
        }
    }
}
