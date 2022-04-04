﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests
    {
        private readonly Engine _engine = new Engine(new PowerFxConfig());

        [Fact]
        public void CollisionsThrow()
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, new DName("DisplayNum")));

            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("DisplayNum", FormulaType.Date, "NoCollision")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "DisplayNum")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "Num")));
        }

        [Fact]
        public void ImmutableDisplayNameProvider()
        {
            var r1 = new RecordType();

            var r2 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));
            var r3 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Bar"));

            Assert.False(ReferenceEquals(r2._type.DisplayNameProvider, r3._type.DisplayNameProvider));
        }

        [Fact]
        public void DisableDisplayNames()
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));

            var r2 = new RecordType()
                .Add(new NamedFormulaType("Other", FormulaType.String, "Foo"));

            Assert.IsType<SingleSourceDisplayNameProvider>(r1._type.DisplayNameProvider);

            var disabledType = DType.AttachOrDisableDisplayNameProvider(r1._type, r2._type.DisplayNameProvider);

            Assert.IsType<DisabledDisplayNameProvider>(disabledType.DisplayNameProvider);
        }

        [Theory]
        [InlineData("If(B, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("Sum(Nested, Inner)", "Sum(NestedDisplay, InnerDisplay)", true)]
        [InlineData("Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", "Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(B, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(DisplayB, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("Sum(NestedDisplay, InnerDisplay)", "Sum(Nested, Inner)", false)]
        [InlineData("Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", "Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", false)]
        public void ValidateDisplayNames(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested", 
                    new TableType().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")), 
                    "NestedDisplay"));

            if (toDisplay)
            {
                var outDisplayExpression = _engine.GetDisplayExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = _engine.GetInvariantExpression(outputExpression, r1);
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }

        [Fact]
        public void ConvertToDisplayNamesNoNames()
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = _engine.GetDisplayExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToInvariantNamesNoNames()
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = _engine.GetInvariantExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }
    }
}
