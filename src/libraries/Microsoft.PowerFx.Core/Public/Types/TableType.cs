// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class TableType : AggregateType
    {
        internal TableType(DType type) : base(type)
        {
            Contract.Assert(type.IsTable);
        }

        public TableType() : base(DType.EmptyTable)
        {
        }

        internal static TableType FromRecord(RecordType type)
        {
            var tableType = type._type.ToTable();
            return new TableType(tableType);
        }


        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public TableType Add(NamedFormulaType field)
        {
            var newType = _type.Add(field._typedName);
            return new TableType(newType);
        }

        public string SingleColumnFieldName => GetNames().First().Name;
        public FormulaType SingleColumnFieldType => GetNames().First().Type;

        public RecordType ToRecord()
        {
            return new RecordType(this._type.ToRecord());
        }
    }
}
