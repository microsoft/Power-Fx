// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class RuntimeErrorTests
    {
        [Theory]
        [InlineData("1/0", "Error 1-2: Invalid operation: division by zero.", "Error 1-2: Operação inválida: divisão por zero.")]
        public void RuntimeErrorLocalizedTests(string expression, string expectedInvariant, string expectedLocale)
        {
            var culture = CultureInfo.CreateSpecificCulture("pt-BR");
            var engine = new RecalcEngine();
            var check = engine.Check(expression);
            var evaluator = check.GetEvaluator();

            var result = evaluator.Eval();            

            if (result is StringValue stringValue)
            {
                var resultLocale = (StringValue)evaluator.Eval(new RuntimeConfig(null, culture));

                Assert.Equal(expectedInvariant, stringValue.Value);
                Assert.Equal(expectedLocale, resultLocale.Value);
            }
            else if (result is ErrorValue errorValue)
            {
                // Host can call GetMessageInLocale to get a localized message.
                Assert.Equal(expectedInvariant, errorValue.Errors.First().ToString());
                Assert.Equal(expectedLocale, errorValue.Errors.First().GetMessageInLocale(culture, true));

                // This should result in the same message as the invariant one.
                Assert.Equal(expectedInvariant, errorValue.Errors.First().ToString());
            }
        }

        [Theory]
        [InlineData("IfError(1/0, FirstError.Message, \"no error\")", "Operação inválida: divisão por zero.")]
        [InlineData("IfError(myerror,Concat(AllErrors,Message,\", \"))", "Operação inválida: divisão por zero., Nome inválido. 'My invalid name' não é reconhecido.")]
        public void RuntimeIfErrorTests(string expression, string expected)
        {
            var error = new ErrorValue(IRContext.NotInSource(FormulaType.String), new List<ExpressionError>()
            {
                new ExpressionError()
                {
                    Kind = ErrorKind.Div0,
                    Severity = ErrorSeverity.Severe,
                    ResourceKey = RuntimeStringResources.ErrDivByZero
                },
                new ExpressionError()
                {
                    Kind = ErrorKind.Div0,
                    Severity = ErrorSeverity.Severe,
                    ResourceKey = RuntimeStringResources.ErrNameIsNotValid,
                    MessageArgs = new object[] { "My invalid name" }
                },
            });

            var culture = CultureInfo.CreateSpecificCulture("pt-BR");
            var engine = new RecalcEngine();

            engine.UpdateVariable("myerror", error);

            var check = engine.Check(expression);
            var result = (StringValue)check.GetEvaluator().Eval(runtimeConfig: new RuntimeConfig(null, culture));
            Assert.Equal(expected, result.Value);
        }
    }
}
