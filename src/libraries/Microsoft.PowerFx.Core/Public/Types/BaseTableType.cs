// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class BaseTableType : AggregateType
    {
        internal BaseTableType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsTable);
        }

        public BaseTableType(ITypeIdentity identity, IEnumerable<string> fieldNames)
            : base(identity, fieldNames, true)
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

        public BaseRecordType ToRecord()
        {
            return new RecordType(DType.ToRecord());
        }
    }
}
