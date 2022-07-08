// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class TableType : AggregateType
    {
        internal TableType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsTable);
        }

        public TableType()
            : base(DType.EmptyTable)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        internal string SingleColumnFieldName
        {
            get
            {
                Contracts.Assert(FieldNames.Count() == 1);
                return FieldNames.First();
            }
        }

        internal FormulaType SingleColumnFieldType => GetFieldType(SingleColumnFieldName);

        public RecordType ToRecord()
        {
            return new KnownRecordType(Type.ToRecord());
        }
    }
}
