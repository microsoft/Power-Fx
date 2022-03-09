// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class FunctionCompilationTests
    {
        [Theory]
        [InlineData("Switch(A, 2, \"two\", \"other\")")]
        [InlineData("IfError(Text(A), Switch(FirstError.Kind, ErrorKind.Div0, \"Division by zero\", ErrorKind.Numeric, \"Numeric error\", \"Other error\"))")]
        public void TestSwitchFunctionCompilation(string expression)
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", 15);
            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);
            Assert.Null(check.Errors);
            Assert.Equal(FormulaType.String, check.ReturnType);
        }

        private class ErrorKindEnumFormulaValue : FormulaValue
        {
            private static readonly KeyValuePair<DName, object>[] ErrorKindValues = new[]
            {
                new KeyValuePair<DName, object>(new DName("Div0"), 13)
            };

            private static readonly FormulaType ErrorKindEnumType = new EnumType(DType.Number, ErrorKindValues);

            internal ErrorKindEnumFormulaValue()
                : base(IRContext.NotInSource(ErrorKindEnumType))
            {
            }

            public override object ToObject()
            {
                return "<ErrorKindEnum>";
            }

            public override void Visit(IValueVisitor visitor)
            {
            }
        }

        [Fact]
        public void ErrorKindCanBeTypedEnumKind()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", new ErrorKindEnumFormulaValue());
            var expression = "Error({Kind:A})";
            var check = engine.Check(expression);
            Assert.Null(check.Errors);
        }
    }
}
