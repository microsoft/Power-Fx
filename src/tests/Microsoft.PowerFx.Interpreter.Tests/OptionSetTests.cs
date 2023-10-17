// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class OptionSetTests : PowerFxTest
    {
        [Fact]
        public async Task OptionSetValueFilterTestAsync()
        {
            var optionSet = new EnumSymbol(
                new DName("os"),
                DType.Boolean,
                new Dictionary<string, object>()
                {
                    { "Yes", true },
                    { "No", false },
                });
            Assert.True(optionSet.TryGetValue(new DName("Yes"), out var optionSetTrueValue));
            Assert.True(optionSet.TryGetValue(new DName("No"), out var optionSetFalseValue));

            var osvType = new OptionSetValueType(optionSet);
            var record = RecordType.Empty().Add("optionSet", osvType);

            var symbols = new SymbolTable();
            var tableSlot = symbols.AddVariable("table", record.ToTable());

            var engine = new RecalcEngine();
            var check = engine.Check("Filter(table, optionSet)", null, symbols);
            Assert.True(check.IsSuccess);

            var symVal = new SymbolValues(symbols);
            var tableValue = FormulaValue.NewTable(
                record, 
                FormulaValue.NewRecordFromFields(record, new NamedValue("optionSet", optionSetTrueValue)),
                FormulaValue.NewRecordFromFields(record, new NamedValue("optionSet", optionSetFalseValue)));
            symVal.Set(tableSlot, tableValue);

            var result = await check.GetEvaluator().EvalAsync(cancellationToken: default, symVal).ConfigureAwait(false);

            var resultTable = Assert.IsAssignableFrom<TableValue>(result);

            Assert.Single(resultTable.Rows);
        }
    }
}
