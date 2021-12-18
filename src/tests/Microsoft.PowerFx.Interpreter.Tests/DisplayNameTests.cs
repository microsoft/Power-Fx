// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests
{    
    public class DisplayNameTests 
    {

        [Fact]
        public void CollisionsThrow()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum");

            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("DisplayNum", FormulaType.Date), "NoCollision"));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date), "DisplayNum"));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date), "Num"));
        }

        [Fact]
        public void ConvertToDisplayNames()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");

            var displayExpressions = engine.GetDisplayExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(DisplayB, DisplayNum, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToDisplayNamesNoNames()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = engine.GetDisplayExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }
            

        [Fact]
        public void ConvertToDisplayNameIsNoOp()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");

            var displayExpressions = engine.GetDisplayExpression("If(DisplayB, DisplayNum, 1234)", r1);

            Assert.Equal("If(DisplayB, DisplayNum, 1234)", displayExpressions);
        }
            

        [Fact]
        public void ConvertToDisplayNameMixed()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");

            var displayExpressions = engine.GetDisplayExpression("If(DisplayB, Num, 1234)", r1);

            Assert.Equal("If(DisplayB, DisplayNum, 1234)", displayExpressions);
        }
            

        [Fact]
        public void ConvertToDisplayNameNested()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB")
                .Add(new NamedFormulaType("Nested", new TableType()
                    .Add(new NamedFormulaType("Inner", FormulaType.Number), "InnerDisplay"))
                , "NestedDisplay");

            var displayExpressions = engine.GetDisplayExpression("Sum(Nested, Inner)", r1);

            Assert.Equal("Sum(NestedDisplay, InnerDisplay)", displayExpressions);
        }
        
        [Fact]
        public void ConvertToInvariantNames()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");
            
            var displayExpressions = engine.GetInvariantExpression("If(DisplayB, DisplayNum, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToInvariantNamesNoNames()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = engine.GetInvariantExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }
            

        [Fact]
        public void ConvertToInvariantNameIsNoOp()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");

            var displayExpressions = engine.GetInvariantExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }
            

        [Fact]
        public void ConvertToInvariantyNameMixed()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB");

            var displayExpressions = engine.GetInvariantExpression("If(DisplayB, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToInvariantNameNested()
        {
            var engine = new RecalcEngine();
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number), "DisplayNum")
                .Add(new NamedFormulaType("B", FormulaType.Boolean), "DisplayB")
                .Add(new NamedFormulaType("Nested", new TableType()
                    .Add(new NamedFormulaType("Inner", FormulaType.Number), "InnerDisplay"))
                , "NestedDisplay");

            var displayExpressions = engine.GetInvariantExpression("Sum(NestedDisplay, InnerDisplay)", r1);

            Assert.Equal("Sum(Nested, Inner)", displayExpressions);
        }
    }
}
