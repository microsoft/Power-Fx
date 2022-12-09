// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class StringType : FormulaType
    {
        public StringType()
            : base(new DType(DKind.String))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "String";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append("\"\"");
        }
    }
}
