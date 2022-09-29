// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Drawing;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class ColorValue : PrimitiveValue<Color>
    {
        internal ColorValue(IRContext irContext, Color value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Color);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToExpression()
        {
            throw new System.NotImplementedException();
        }
    }
}
