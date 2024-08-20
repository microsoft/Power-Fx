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
        [Fact]
        public void RuntimeErrorLocalizedTests()
        {
            var engine = new RecalcEngine();
            var check = new CheckResult(engine)
                .SetText("1/0")
                .SetBindingInfo()
                .SetDefaultErrorCulture(CultureInfo.CreateSpecificCulture("pt-BR"));

            check.ApplyBinding();

            var result = (ErrorValue)check.GetEvaluator().Eval();

            Assert.Equal("ErrDivByZero", result.Errors.First().MessageKey);
            Assert.Equal("Error 1-2: Operação inválida: divisão por zero.", result.Errors.First().ToString());
        }
    }
}
