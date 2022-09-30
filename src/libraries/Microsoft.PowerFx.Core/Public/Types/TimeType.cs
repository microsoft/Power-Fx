// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class TimeType : FormulaType
    {
        internal TimeType()
            : base(DType.Time)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Time";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            var timeSpanMin = TimeSpan.MinValue;

            sb.Append($"Time({timeSpanMin.Hours},{timeSpanMin.Minutes},{timeSpanMin.Seconds},{timeSpanMin.Milliseconds})");
        }
    }
}
