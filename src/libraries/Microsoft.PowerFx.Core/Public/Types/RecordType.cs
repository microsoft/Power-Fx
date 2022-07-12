// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public sealed class RecordType : BaseRecordType
    {
        internal RecordType(DType type)
            : base(type)
        {
        }

        public RecordType()
            : base(DType.EmptyRecord)
        {
        }

        public RecordType Add(NamedFormulaType field)
        {
            return new RecordType(AddFieldToType(field));
        }

        public RecordType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type.DType, new DName(logicalName)), optionalDisplayName));
        }
    }
}
