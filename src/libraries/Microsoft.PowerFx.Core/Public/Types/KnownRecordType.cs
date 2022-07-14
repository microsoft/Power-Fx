// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    internal sealed class KnownRecordType : RecordType
    {
        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);

        internal KnownRecordType(DType type)
            : base(type)
        {
        }

        internal KnownRecordType()
            : base(DType.EmptyRecord)
        {
        }
    }
}
