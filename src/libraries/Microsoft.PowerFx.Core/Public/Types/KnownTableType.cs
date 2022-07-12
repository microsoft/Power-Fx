// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public sealed class KnownTableType : TableType
    {
        internal KnownTableType(DType type)
            : base(type)
        {
        }

        public KnownTableType()
            : base(DType.EmptyTable)
        {
        }

        internal static KnownTableType FromRecord(BaseRecordType type)
        {
            var tableType = type.DType.ToTable();
            return new KnownTableType(tableType);
        }

        public KnownTableType Add(NamedFormulaType field)
        {
            return new KnownTableType(AddFieldToType(field));
        }

        public KnownTableType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type.DType, new DName(logicalName)), optionalDisplayName));
        }
    }
}
