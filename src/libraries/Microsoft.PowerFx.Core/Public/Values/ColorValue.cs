// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Drawing;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
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
    }
}
