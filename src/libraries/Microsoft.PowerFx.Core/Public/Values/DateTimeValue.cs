// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Date and Time together, in the local time zone.
    /// </summary>
    public class DateTimeValue : PrimitiveValue<DateTime>
    {
        internal DateTimeValue(IRContext irContext, DateTime value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.DateTime);
            Contract.Assert(value.Kind != DateTimeKind.Utc);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToExpression()
        {
            var ret = Value.Kind == DateTimeKind.Utc ? Value : Value.ToUniversalTime();

            return $"DateTimeValue({CharacterUtils.ToPlainText(Value.ToString("o"))})";
        }
    }
}
