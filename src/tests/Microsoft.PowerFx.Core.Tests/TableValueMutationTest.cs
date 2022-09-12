// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(1)));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(2)));
            var t1 = FormulaValue.NewTable(r1.Type, r1);

            Assert.Equal(1, t1.Count());

            // succeeds
            await t1.AppendAsync(r2);

            Assert.Equal(2, t1.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.AppendAsync(r2);

            Assert.True(result.IsError);
            Assert.Equal("AppendAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }

        [Fact]
        public async Task RemoveTest()
        {
            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(1)));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(2)));
            var t1 = FormulaValue.NewTable(r1.Type, r1);

            // succeeds
            await t1.AppendAsync(r2);
            await t1.RemoveAsync(new RecordValue[] { r1 }, false);

            Assert.Equal(1, t1.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.RemoveAsync(new RecordValue[] { r1 }, false);

            Assert.True(result.IsError);
            Assert.Equal("RemoveAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }

        [Fact]
        public async Task PatchTest()
        {
            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(1)));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(2)));

            var t1 = FormulaValue.NewTable(r1.Type, r1);

            // succeeds
            await t1.PatchAsync(r1, r2);

            var firstRecord = t1.Index(1);

            Assert.Equal(1, t1.Count());
            Assert.NotNull(firstRecord);
            Assert.Equal(r2.ToObject().ToString(), firstRecord.Value.ToObject().ToString());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.PatchAsync(r1, r2);

            Assert.True(result.IsValue);

            // There is no change to the IEnumerable object. Therefore, no error is created.
            //Assert.True(result.IsError);
            //Assert.Equal("PatchAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }
    }
}
