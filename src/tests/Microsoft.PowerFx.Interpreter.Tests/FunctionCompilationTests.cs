// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
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

        private class ErrorKindEnumFormulaType : FormulaType
        {
            private static readonly DType _errorKindEnumType = new EnumStore().GetEnum("ErrorKind");

            public ErrorKindEnumFormulaType()
                : base(_errorKindEnumType)
            {
            }

            public override void Visit(ITypeVistor vistor)
            {
            }
        }

        private class ErrorKindEnumFormulaValue : FormulaValue
        {
            internal ErrorKindEnumFormulaValue()
                : base(IRContext.NotInSource(new ErrorKindEnumFormulaType()))
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
