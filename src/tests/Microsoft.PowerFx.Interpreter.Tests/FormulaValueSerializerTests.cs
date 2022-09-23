// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    /* !JYL! How to serialize and test:
     * ErroraValeu
     * UntypedObjectValue
     * OptionSetValue
     */

    public class FormulaValueSerializerTests : PowerFxTest
    {
        [Fact]
        public async Task PrimitiveValueSerializerTest()
        {
            var dateTimeNow = DateTime.Parse("12/25/2022 12:34:56");
            var newGuid = Guid.Parse("811bfd49-38f6-4fdd-9993-4ed7e7bef277");

            var numberValue = FormulaValue.New(1);
            var boolValue = FormulaValue.New(true);
            var colorValue = new ColorValue(IRContext.NotInSource(FormulaType.Color), Color.Blue);
            var dateTimeValue = FormulaValue.New(dateTimeNow);
            var dateValue = FormulaValue.NewDateOnly(dateTimeNow.Date);
            var guidValue = FormulaValue.New(newGuid);
            var stringValue = FormulaValue.New("*SERIALIZER*");
            var timeValue = FormulaValue.New(dateTimeNow.TimeOfDay);

            Assert.Equal("1", numberValue.ToExpression());
            Assert.Equal("true", boolValue.ToExpression());
            Assert.Equal("\"*SERIALIZER*\"", stringValue.ToExpression());
            Assert.Equal("DateTime(2022,12,25,12,34,56)", dateTimeValue.ToExpression());
            Assert.Equal("Date(2022,12,25)", dateValue.ToExpression());
            Assert.Equal("Time(12,34,56)", timeValue.ToExpression());
            Assert.Equal("GUID(\"811bfd49-38f6-4fdd-9993-4ed7e7bef277\")", guidValue.ToExpression()); // !JYL! Test with Blank()?

            var engine = new RecalcEngine();

            var result = await engine.EvalAsync(numberValue.ToExpression(), CancellationToken.None);
            Assert.Equal(1.0, result.ToObject());

            result = await engine.EvalAsync(boolValue.ToExpression(), CancellationToken.None);
            Assert.Equal(true, result.ToObject());

            result = await engine.EvalAsync(stringValue.ToExpression(), CancellationToken.None);
            Assert.Equal("*SERIALIZER*", result.ToObject());

            result = await engine.EvalAsync(dateTimeValue.ToExpression(), CancellationToken.None);
            Assert.Equal("12/25/2022 12:34:56 PM", result.ToObject().ToString());

            result = await engine.EvalAsync(dateValue.ToExpression(), CancellationToken.None);
            Assert.Equal("12/25/2022 12:00:00 AM", result.ToObject().ToString());

            result = await engine.EvalAsync(timeValue.ToExpression(), CancellationToken.None);
            Assert.Equal("12:34:56", result.ToObject().ToString());

            result = await engine.EvalAsync(guidValue.ToExpression(), CancellationToken.None);
            Assert.Equal("811bfd49-38f6-4fdd-9993-4ed7e7bef277", ((Guid)result.ToObject()).ToString("D"));

            // !JYL! Color?
        }

        [Fact]
        public async Task RecordValueSerializerTest()
        {
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(12)),
                new NamedValue("Field2 Field2", FormulaValue.New(34)));

            Assert.Equal("{'Field1':12,'Field2 Field2':34}", record.ToExpression());

            var engine = new RecalcEngine();

            var result = await engine.EvalAsync(record.ToExpression(), CancellationToken.None);
            Assert.Equal("{Field1:12,Field2 Field2:34}", result.Dump());
        }

        [Fact]
        public async Task TableValueSerializerTest()
        {
            var record1 = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(12)),
                new NamedValue("Field2 2", FormulaValue.New(34)));

            var record2 = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(21)),
                new NamedValue("Field2 2", FormulaValue.New(43)));

            var table = FormulaValue.NewTable(record1.Type, new List<RecordValue>() { record1, record2 });

            Assert.Equal("Table({'Field1':12,'Field2 2':34},{'Field1':21,'Field2 2':43})", table.ToExpression());

            var engine = new RecalcEngine();

            var result = await engine.EvalAsync(table.ToExpression(), CancellationToken.None);
            Assert.Equal("Table({Field1:12,Field2 2:34},{Field1:21,Field2 2:43})", result.Dump());
        }

        [Fact]
        public void ErrorValueSerializerTest()
        {
            var errorValue = FormulaValue.NewError(
                new ExpressionError()
                {
                    Kind = ErrorKind.ReadOnlyValue,
                    Severity = ErrorSeverity.Critical,
                    Message = "Something went wrong"
                });

            Assert.Throws<NotImplementedException>(() => errorValue.ToExpression());
        }

        [Fact]
        public async Task BlankValueSerializerTest()
        {
            var blankValue = FormulaValue.NewBlank();

            Assert.Equal("Blank()", blankValue.ToExpression());

            var engine = new RecalcEngine();

            var result = await engine.EvalAsync(blankValue.ToExpression(), CancellationToken.None);
            Assert.Equal("Blank()", result.Dump());
        }
    }
}
