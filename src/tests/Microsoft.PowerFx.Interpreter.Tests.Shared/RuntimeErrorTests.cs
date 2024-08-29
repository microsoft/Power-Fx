// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class RuntimeErrorTests
    {
        [Theory]
        [InlineData("1/0", "Error 1-2: Invalid operation: division by zero.", "Error 1-2: Operação inválida: divisão por zero.")]
        [InlineData("IfError(1/0, FirstError.Message, \"no error\")", "Invalid operation: division by zero.", "Operação inválida: divisão por zero.")]

        // Aggregators
        [InlineData("Average([1/0,2,3], Value)", "Error 10-11: Invalid operation: division by zero.", "Error 10-11: Operação inválida: divisão por zero.")]
        [InlineData("Average(1/0,2,3)", "Error 9-10: Invalid operation: division by zero.", "Error 9-10: Operação inválida: divisão por zero.")]
        [InlineData("Average(Blank())", "Error 0-16: Invalid operation: division by zero.", "Error 0-16: Operação inválida: divisão por zero.")]
        [InlineData("Sum(1/0,2)", "Error 5-6: Invalid operation: division by zero.", "Error 5-6: Operação inválida: divisão por zero.")]
        [InlineData("Sum([1/0,2],Value)", "Error 6-7: Invalid operation: division by zero.", "Error 6-7: Operação inválida: divisão por zero.")]
        [InlineData("VarP([1/0,2,3], Value)", "Error 7-8: Invalid operation: division by zero.", "Error 7-8: Operação inválida: divisão por zero.")]
        [InlineData("VarP(1/0,2,3)", "Error 6-7: Invalid operation: division by zero.", "Error 6-7: Operação inválida: divisão por zero.")]
        [InlineData("VarP(Blank())", "Error 0-13: Invalid operation: division by zero.", "Error 0-13: Operação inválida: divisão por zero.")]
        [InlineData("StdevP([1/0,2,3], Value)", "Error 9-10: Invalid operation: division by zero.", "Error 9-10: Operação inválida: divisão por zero.")]
        [InlineData("StdevP(1/0,2,3)", "Error 8-9: Invalid operation: division by zero.", "Error 8-9: Operação inválida: divisão por zero.")]
        [InlineData("StdevP(Blank())", "Error 0-15: Invalid operation: division by zero.", "Error 0-15: Operação inválida: divisão por zero.")]
        [InlineData("Max([1/0,2,3], Value)", "Error 6-7: Invalid operation: division by zero.", "Error 6-7: Operação inválida: divisão por zero.")]
        [InlineData("Max(1/0,2,3)", "Error 5-6: Invalid operation: division by zero.", "Error 5-6: Operação inválida: divisão por zero.")]
        [InlineData("Min([1/0,2,3], Value)", "Error 6-7: Invalid operation: division by zero.", "Error 6-7: Operação inválida: divisão por zero.")]
        [InlineData("Min(1/0,2,3)", "Error 5-6: Invalid operation: division by zero.", "Error 5-6: Operação inválida: divisão por zero.")]
        [InlineData("Mod(1,0)", "Error 0-8: Invalid operation: division by zero.", "Error 0-8: Operação inválida: divisão por zero.")]
        public void RuntimeErrorLocalizedTests(string expression, string expectedInvariant, string expectedLocale)
        {
            var culture = CultureInfo.CreateSpecificCulture("pt-BR");
            var engine = new RecalcEngine();
            var check = engine.Check(expression);
            var evaluator = check.GetEvaluator();

            //string message = myError.Errors.First().GetMessageInLocale(LOCALE)

            var result = evaluator.Eval();            

            if (result is StringValue stringValue)
            {
                var resultLocale = (StringValue)evaluator.Eval(new RuntimeConfig(null, culture));

                Assert.Equal(expectedInvariant, stringValue.Value);
                Assert.Equal(expectedLocale, resultLocale.Value);
            }
            else if (result is ErrorValue errorValue)
            {
                Assert.Equal(expectedInvariant, errorValue.Errors.First().ToString());
                Assert.Equal(expectedLocale, errorValue.Errors.First().GetMessageInLocale(culture, true));
            }
        }
    }
}
