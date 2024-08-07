// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public sealed class RecordValueTests
    {
        [Fact]
        public void GetOriginalFieldsTest()
        {
            var recordType = RecordType.Empty()
                .Add("a", FormulaType.Decimal)
                .Add("b", FormulaType.String);

            var tableType = recordType
                .Add("c", FormulaType.Boolean)
                .ToTable();

            var recordValue = new InMemoryRecordValue(
                IRContext.NotInSource(recordType),
                new NamedValue("a", FormulaValue.New(1)),
                new NamedValue("b", FormulaValue.New("string")));

            var wrapped = CompileTimeTypeWrapperRecordValue.AdjustType(tableType.ToRecord(), recordValue);

            // OriginalFields will return hard fields from record type and not soft fields from table type.
            Assert.Equal(2, wrapped.OriginalFields.Count());
            Assert.Equal(3, wrapped.Fields.Count());
        }
    }
}
