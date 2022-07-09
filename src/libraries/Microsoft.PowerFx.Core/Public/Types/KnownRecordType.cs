// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public sealed class KnownRecordType : RecordType
    {
        internal KnownRecordType(DType type)
            : base(type)
        {
        }

        public KnownRecordType()
            : base(DType.EmptyRecord)
        {
        }

        public KnownRecordType Add(NamedFormulaType field)
        {
            return new KnownRecordType(AddFieldToType(field));
        }

        public KnownRecordType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type.Type, new DName(logicalName)), optionalDisplayName));
        }
    }
}
