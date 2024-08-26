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

        //[InlineData("1/0", "Error 1-2: Invalid operation: division by zero.", "Error 1-2: Operação inválida: divisão por zero.")]
        //[InlineData("IfError(1/0, FirstError.Message, \"no error\")", "Invalid operation: division by zero.", "Operação inválida: divisão por zero.")]

        // Aggregators
        //[InlineData("Average([1/0,2,3], Value)", "Error 10-11: Invalid operation: division by zero.", "Error 10-11: Operação inválida: divisão por zero.")]
        //[InlineData("Average(1/0,2,3)", "Error 9-10: Invalid operation: division by zero.", "Error 9-10: Operação inválida: divisão por zero.")]
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
            var engine = new RecalcEngine();
            var check = engine.Check(expression);
            var evaluator = check.GetEvaluator();

            var resultInvariant = evaluator.Eval();
            var resultLocale = evaluator.Eval(new RuntimeConfig(null, CultureInfo.CreateSpecificCulture("pt-BR")));

            if (resultInvariant is StringValue stringInvariant)
            {
                var stringLocale = resultLocale as StringValue;

                Assert.Equal(expectedInvariant, stringInvariant.Value);
                Assert.Equal(expectedLocale, stringLocale.Value);
            }
            else if (resultInvariant is ErrorValue errorInvariant)
            {
                var errorLocale = resultLocale as ErrorValue;

                Assert.Equal(expectedInvariant, errorInvariant.Errors.First().ToString());
                Assert.Equal(expectedLocale, errorLocale.Errors.First().ToString());
            }
        }
    }
}
