// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class RecordType : AggregateType
    {
        internal RecordType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsRecord);
        }

        public RecordType()
            : base(DType.EmptyRecord)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }
    }
}
