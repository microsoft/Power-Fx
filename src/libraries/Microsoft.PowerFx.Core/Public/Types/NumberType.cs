// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class NumberType : FormulaType
    {
        internal NumberType()
            : base(new DType(DKind.Number))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Number";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append("0");
        }

        internal override void DefaultUniqueExpressionValue(StringBuilder sb)
        {
            sb.Append("Float(0)");
        }
    }

    public class DecimalType : FormulaType
    {
        internal DecimalType()
            : base(new DType(DKind.Decimal))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Decimal";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append("0");
        }

        internal override void DefaultUniqueExpressionValue(StringBuilder sb)
        {
            sb.Append("Decimal(0)");
        }
    }
}
