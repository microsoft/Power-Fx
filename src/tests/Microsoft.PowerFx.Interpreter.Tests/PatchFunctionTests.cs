// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PatchFunctionTests : PowerFxTest
    {
        [Theory]
        [InlineData(typeof(PatchFunction))]
        [InlineData(typeof(PatchRecordFunction))]
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
            var result = await function.InvokeAsync(args, CancellationToken.None);

            Assert.IsType<ErrorValue>(result);
        }
    }
}
