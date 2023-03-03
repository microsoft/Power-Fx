// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class NumberValue : PrimitiveValue<double>
    {
        internal NumberValue(IRContext irContext, double value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Number);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append((Value == 0) ? "0" : Value.ToString(CultureInfo.CurrentCulture));
        }
    }
}
