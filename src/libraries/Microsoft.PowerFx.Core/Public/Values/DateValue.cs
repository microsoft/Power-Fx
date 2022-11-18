// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
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
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"Date({Value.Year},{Value.Month},{Value.Day})");
        }
    }
}
