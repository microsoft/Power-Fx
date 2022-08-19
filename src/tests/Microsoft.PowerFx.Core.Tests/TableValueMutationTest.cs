// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class TableValueMutationTest : PowerFxTest
    {
        [Fact]
        public async Task AppendRecordTest()
        {
            var r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("f1", FormulaValue.New(1)));

            var r2 = FormulaValue.NewRecordFromFields(
                new NamedValue("f1", FormulaValue.New(2)));

            var t = FormulaValue.NewTable(r1.Type, r1); // Mutable 

            Assert.Equal(1, t.Count());
            await t.AppendAsync(r2); // succeeds

            Assert.Equal(2, t.Count());

            // Future test case
            //IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            //var t2 = FormulaValue.NewTable(r1.Type, source); // Immutabe
            //await t2.AppendAsync(r2); // Fails 
        }
    }
}
