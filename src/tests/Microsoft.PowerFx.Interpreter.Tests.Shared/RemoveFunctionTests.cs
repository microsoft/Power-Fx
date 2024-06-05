﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
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
    public class RemoveFunctionTests : PowerFxTest
    {
        [Fact]
        public async Task RemoveRecordTest()
        {
            var r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("f1", FormulaValue.New(1)),
                new NamedValue("f2", FormulaValue.New("earth")),
                new NamedValue("f3", FormulaValue.New(true)));

            var r2 = FormulaValue.NewRecordFromFields(
                new NamedValue("f1", FormulaValue.New(2)),
                new NamedValue("f2", FormulaValue.New("moon")),
                new NamedValue("f3", FormulaValue.New(true)));

            var r3 = FormulaValue.NewRecordFromFields(
                new NamedValue("f1", FormulaValue.New(3)),
                new NamedValue("f2", FormulaValue.New("mars")),
                new NamedValue("f3", FormulaValue.New(false)));

            var rRemove_error = FormulaValue.NewRecordFromFields(
                new NamedValue("f3", FormulaValue.New(true)));

            var t1 = FormulaValue.NewTable(r1.Type, r1, r2, r3);

            // Remove single
            var result_error = await t1.RemoveAsync(new List<RecordValue>() { rRemove_error }, false, CancellationToken.None);

            Assert.IsType<ErrorValue>(result_error.ToFormulaValue());
            Assert.Equal(3, t1.Count());

            var list = new List<RecordValue>() { r1 };

            await t1.RemoveAsync(list, false, CancellationToken.None);
            Assert.Equal(2, t1.Count());

            var t2 = FormulaValue.NewTable(r1.Type, r1, r2, r3);

            // Remove all
            await t2.RemoveAsync(list, true, CancellationToken.None);

            Assert.Equal(2, t2.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1, r2, r3 };
            var t3 = FormulaValue.NewTable(r1.Type, source);
            var result = await t3.RemoveAsync(list, false, CancellationToken.None);

            Assert.True(result.IsError);
            Assert.Equal("RemoveAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }
    }
}
