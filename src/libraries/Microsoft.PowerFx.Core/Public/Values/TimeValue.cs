// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    public class TimeValue : PrimitiveValue<TimeSpan>
    {
        internal TimeValue(IRContext irContext, TimeSpan ts)
            : base(irContext, ts)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Time);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
