// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using System;
using System.Diagnostics.Contracts;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represents a Date and Time together, in the local time zone
    /// </summary>
    public class DateTimeValue : PrimitiveValue<DateTime>
    {
        internal DateTimeValue(IRContext irContext, DateTime value) : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.DateTime);
            Contract.Assert(value.Kind != DateTimeKind.Utc);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
