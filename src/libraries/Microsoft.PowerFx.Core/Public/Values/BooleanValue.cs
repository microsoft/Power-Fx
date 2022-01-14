// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    public class BooleanValue : PrimitiveValue<bool>
    {
        internal BooleanValue(IRContext irContext, bool value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Boolean);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
