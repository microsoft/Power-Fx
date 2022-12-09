// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class BooleanType : FormulaType
    {
        internal BooleanType()
            : base(new DType(DKind.Boolean))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Boolean";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append("false");
        }
    }
}
