﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
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
