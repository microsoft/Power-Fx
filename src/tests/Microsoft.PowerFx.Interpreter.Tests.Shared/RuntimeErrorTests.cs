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
