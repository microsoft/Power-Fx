﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
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

        public override string ToExpression()
        {
            return $"Time({Value.Hours},{Value.Minutes},{Value.Seconds},{Value.Milliseconds})";
        }
    }
}
