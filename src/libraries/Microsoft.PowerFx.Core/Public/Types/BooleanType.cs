// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class BooleanType : FormulaType
    {
        public override DName Name => new DName("Boolean");

        internal BooleanType()
            : base(DType.Boolean)
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
