// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class TimeValue : PrimitiveValue<TimeSpan>
    {
        private const string ExpressionFormat = "Time({0},{1},{2},{3})";

        internal TimeValue(IRContext irContext, TimeSpan ts)
            : base(irContext, ts)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Time);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append(string.Format(CultureInfo.InvariantCulture, ExpressionFormat, Value.Hours, Value.Minutes, Value.Seconds, Value.Milliseconds));
        }
    }
}
