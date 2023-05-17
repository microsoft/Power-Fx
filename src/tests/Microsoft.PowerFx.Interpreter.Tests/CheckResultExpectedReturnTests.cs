// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class CheckResultExpectedReturnTests : PowerFxTest
    {
        [Theory]
        [InlineData("1.2", true, "decimal", "")]
        [InlineData("203", true, "decimal", "")]
        [InlineData("Decimal(1.2)", true, "decimal", "")]
        [InlineData("Float(203)", true, "number", "")]
        [InlineData("\"12\"", false, "string", "The type of this expression does not match the expected type 'Number, Decimal'")]
        [InlineData("{a:1, b:2}", false, "record", "The type of this expression does not match the expected type 'Number, Decimal'")]
        public void ExpectedReturnNumberDecimalValueTest(string inputExp, bool isSuccess, string expectedType, string errorMsg)
        {
            var expectedReturnTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal };
            CheckResultExpectedReturnType(inputExp, isSuccess, errorMsg, expectedReturnTypes, GetFormularType(expectedType));
        }

        [Theory]
        [InlineData("1.2", true, "decimal", "")]
        [InlineData("203", true, "decimal", "")]
        [InlineData("Decimal(1.2)", true, "decimal", "")]
        [InlineData("Float(203)", true, "number", "")]
        [InlineData("\"12\"", true, "string", "")]
        [InlineData("{a:1, b:2}", false, "record", "The type of this expression does not match the expected type 'Number, Decimal, Text'")]
        public void ExpectedReturnNumberDecimalStringValueTest(string inputExp, bool isSuccess, string expectedType, string errorMsg)
        {
            var expectedReturnTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal, FormulaType.String };
            CheckResultExpectedReturnType(inputExp, isSuccess, errorMsg, expectedReturnTypes, GetFormularType(expectedType));
        }

        private void CheckResultExpectedReturnType(string inputExp, bool isSuccess, string errorMsg, FormulaType[] expectedReturnTypes, FormulaType expectedType)
        {
            var engine = new RecalcEngine();

            var scope = new EditorContextScope((expression) => new CheckResult(engine)
                .SetText(expression)
                .SetBindingInfo()
                .SetExpectedReturnValue(expectedReturnTypes));

            var check = scope.Check(inputExp);

            if (isSuccess)
            {
                Assert.True(check.IsSuccess);
                Assert.Equal(expectedType, check.ReturnType);
            }
            else
            {
                string exMsg = null;

                try
                {
                    var errors = check.ApplyErrors();
                    exMsg = errors.First().Message;
                    Assert.False(check.IsSuccess);
                }
                catch (Exception ex)
                {
                    exMsg = ex.ToString();
                }

                Assert.Contains(errorMsg, exMsg);
            }
        }

        private FormulaType GetFormularType(string type)
        {
            switch (type)
            {
                case "decimal":
                    return FormulaType.Decimal;
                case "number":
                    return FormulaType.Number;
                case "string":
                    return FormulaType.String;
                default:
                    return FormulaType.Blank;
            }
        }
    }
}
