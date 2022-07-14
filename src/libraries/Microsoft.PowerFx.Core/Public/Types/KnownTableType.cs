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
    internal sealed class KnownTableType : TableType
    {
        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);

        internal KnownTableType(DType type)
            : base(type)
        {
        }

        internal KnownTableType()
            : base(DType.EmptyTable)
        {
        }
    }
}
