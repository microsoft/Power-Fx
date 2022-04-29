// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
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
