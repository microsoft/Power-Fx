// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class CheckResultExpectedReturnTests : PowerFxTest
    {
        [Theory]
        [InlineData("1.2", true, "")]
        [InlineData("203", true, "")]
        [InlineData("Decimal(1.2)", true, "")]
        [InlineData("Float(203)", true, "")]
        [InlineData("\"12\"", false, "The type of this expression does not match the expected type 'Number'")]
        [InlineData("{a:1, b:2}", false, "The type of this expression does not match the expected type 'Number'")]
        public void ExpectedReturnNumberValueTest(string inputExp, bool isSuccess, string errorMsg)
        {
            var allowTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal };
            CheckResultExpectedReturnType(inputExp, isSuccess, errorMsg, allowTypes, FormulaType.Number);
        }

        [Theory]
        [InlineData("1.2", true, "")]
        [InlineData("203", true, "")]
        [InlineData("Decimal(1.2)", true, "")]
        [InlineData("Float(203)", true, "")]
        [InlineData("\"12\"", true, "")]
        [InlineData("{a:1, b:2}", false, "The type of this expression does not match the expected type 'Decimal'")]
        public void ExpectedReturnDecimalValueTest(string inputExp, bool isSuccess, string errorMsg)
        {
            var allowTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal, FormulaType.String };
            CheckResultExpectedReturnType(inputExp, isSuccess, errorMsg, allowTypes, FormulaType.Decimal);
        }

        private void CheckResultExpectedReturnType(string inputExp, bool isSuccess, string errorMsg, FormulaType[] allowTypes, FormulaType expectedReturnType)
        {
            var engine = new RecalcEngine();

            var scope = new EditorContextScope((expression) => new CheckResult(engine)
                .SetText(expression)
                .SetBindingInfo()
                .SetAllowedInputType(allowTypes)
                .SetExpectedReturnValue(FormulaType.Number, true));

            var check = scope.Check(inputExp);

            if (isSuccess)
            {
                Assert.True(check.IsSuccess);
            }
            else
            {
                string exMsg = null;

                try
                {
                    var errors = check.ApplyErrors();
                    exMsg = errorMsg.ToString();
                    Assert.False(check.IsSuccess);
                }
                catch (Exception ex)
                {
                    exMsg = ex.ToString();
                }

                Assert.Contains(errorMsg, exMsg);
            }
        }
    }
}
