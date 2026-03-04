// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    // Despite the internal "Number" naming, this is actually the maker facing "Float" type. 
    // The maker facing type name "Number" can mean either Float or Decimal depending on the host's config.
    // "Number" was the term used when Power Fx was created, but we then later added Decimal
    // and didn't want to do the huge public change here and in all the host code that used floating point.
    public class NumberType : FormulaType
    {
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
