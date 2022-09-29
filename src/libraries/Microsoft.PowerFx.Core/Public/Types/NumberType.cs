// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        internal override string DefaultExpressionValue()
        {
            return "0";
        }
    }
}
