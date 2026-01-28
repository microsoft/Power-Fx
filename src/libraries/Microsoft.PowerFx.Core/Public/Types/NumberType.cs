// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class NumberType : FormulaType
    {
        public override DName Name => new DName("Float");

        internal NumberType()
            : base(DType.Number)
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
            sb.Append($"Float(\"0\")");
        }
    }
}
