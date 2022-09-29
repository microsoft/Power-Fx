// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class GuidValue : PrimitiveValue<Guid>
    {
        internal GuidValue(IRContext irContext, Guid value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Guid);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToExpression()
        {
            return $"GUID({CharacterUtils.ToPlainText(Value.ToString("N"))})";
        }
    }
}
