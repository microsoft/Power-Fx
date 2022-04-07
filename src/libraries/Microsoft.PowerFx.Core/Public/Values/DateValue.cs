// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represents a Date only, without a time component, in the local time zone.
    /// </summary>
    public class DateValue : PrimitiveValue<DateTime>
    {
        internal DateValue(IRContext irContext, DateTime value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Date);
            Contract.Assert(value.TimeOfDay == TimeSpan.Zero);
            Contract.Assert(value.Kind != DateTimeKind.Utc);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
