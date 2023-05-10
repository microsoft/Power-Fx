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
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ErrorFunctionTests : PowerFxTest
    {
        [Fact]
        public async Task AllErrorKindsHaveDefaultMessages()
        {
            foreach (ErrorKind errorKind in Enum.GetValues(typeof(ErrorKind)))
            {
                int numericValue = (int)errorKind;
                var expression = $"IfError( Error( {{Kind:ErrorKind.{errorKind}}} ), $\"{{FirstError.Kind}}: {{FirstError.Message}}\" )";
                var engine = new RecalcEngine();
                var result = engine.Eval(expression);
                Assert.IsType<StringValue>(result);
                var stringResult = ((StringValue)result).Value;
                var expectedStart = $"{numericValue}: ";
                Assert.StartsWith(expectedStart, stringResult);
                Assert.True(stringResult.Length > expectedStart.Length); // Message is not empty
                Assert.DoesNotContain($"({numericValue})", stringResult); // Numeric value is not part of the message
            }
        }

        [Theory]
        [InlineData(987)]
        [InlineData(1234)]
        public async Task ErrorKindNotReservedHaveDefaultMessage(int errorKind)
        {
            var expression = $"IfError( Error( {{Kind:{errorKind}}} ), $\"{{FirstError.Kind}}: {{FirstError.Message}}\" )";
            var engine = new RecalcEngine();
            var result = engine.Eval(expression);
            Assert.IsType<StringValue>(result);
            var stringResult = ((StringValue)result).Value;
            var expectedStart = $"{errorKind}: ";
            Assert.StartsWith(expectedStart, stringResult);
            Assert.Contains($"({errorKind})", stringResult);
        }
    }
}
