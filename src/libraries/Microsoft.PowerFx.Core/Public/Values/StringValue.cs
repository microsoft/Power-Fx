// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    public class StringValue : PrimitiveValue<string>
    {
        internal StringValue(IRContext irContext, string value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.String);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal StringValue ToLower()
        {
            return new StringValue(IRContext.NotInSource(FormulaType.String), Value.ToLowerInvariant());
        }
    }
}
