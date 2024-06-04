﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
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
            await t1.AppendAsync(r2, CancellationToken.None);

            Assert.Equal(2, t1.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.AppendAsync(r2, CancellationToken.None);

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
            await t1.AppendAsync(r2, CancellationToken.None);
            await t1.RemoveAsync(new RecordValue[] { r1 }, false, CancellationToken.None);

            Assert.Equal(1, t1.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.RemoveAsync(new RecordValue[] { r1 }, false, CancellationToken.None);

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
            await t1.PatchAsync(r1, r2, CancellationToken.None);

            var firstRecord = t1.Index(1);

            Assert.Equal(1, t1.Count());
            Assert.NotNull(firstRecord);
            Assert.Equal(r2.ToObject().ToString(), firstRecord.Value.ToObject().ToString());

#if false
            // I don't understand this test.  If the source was immutable, then either there 
            // should be an error because Patch isn't doing anything, or source should have been changed?
   
            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.PatchAsync(r1, r2, CancellationToken.None);

            Assert.True(result.IsValue);
#endif

            // There is no change to the IEnumerable object. Therefore, no error is created.
            //Assert.True(result.IsError);
            //Assert.Equal("PatchAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }

        [Fact]
        public async Task ClearTest()
        {
            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(1)));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("f1", FormulaValue.New(2)));

            // Mutable
            var t1 = FormulaValue.NewTable(r1.Type, new List<RecordValue>() { r1, r2 });

            await t1.ClearAsync(CancellationToken.None);

            Assert.Equal(0, t1.Count());

            // Immutable
            IEnumerable<RecordValue> source = new RecordValue[] { r1, r2 };
            var t2 = FormulaValue.NewTable(r1.Type, source);
            var result = await t2.ClearAsync(CancellationToken.None);

            Assert.True(result.IsError);
            Assert.Equal("ClearAsync is not supported on this table instance.", result.Error.Errors[0].Message);
        }
    }
}
