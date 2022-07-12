// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public sealed class TableType : BaseTableType
    {
        internal TableType(DType type)
            : base(type)
        {
        }

        public TableType()
            : base(DType.EmptyTable)
        {
        }

        internal static TableType FromRecord(BaseRecordType type)
        {
            var tableType = type.DType.ToTable();
            return new TableType(tableType);
        }

        public TableType Add(NamedFormulaType field)
        {
            return new TableType(AddFieldToType(field));
        }

        public TableType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type.DType, new DName(logicalName)), optionalDisplayName));
        }
    }
}
