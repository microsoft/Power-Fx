// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class TableType : AggregateType
    {
        internal TableType(DType type)
            : base(type)
        {
            Contract.Assert(type.IsTable);
        }

        [Obsolete("UseTableType.Empty() instead")]
        public TableType()
            : base(DType.EmptyTable)
        {
        }

        internal static TableType FromRecord(RecordType type)
        {
            var tableType = type._type.ToTable();
            return new TableType(tableType);
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public static TableType Empty() => new TableType(DType.EmptyTable);

        public TableType Add(NamedFormulaType field)
        {
            return new TableType(AddFieldToType(field));
        }

        public string SingleColumnFieldName
        {
            get
            {
                Contracts.Assert(GetNames().Count() == 1);
                return GetNames().First().Name;
            }
        }

        public FormulaType SingleColumnFieldType
        {
            get
            {
                Contracts.Assert(GetNames().Count() == 1);
                return GetNames().First().Type;
            }
        }

        public RecordType ToRecord()
        {
            return new RecordType(_type.ToRecord());
        }
    }
}
