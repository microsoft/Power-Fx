// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ResolvedObjectNode : IntermediateNode
    {
        public readonly object Value;

        public ResolvedObjectNode(IRContext irContext, object value)
            : base(irContext)
        {
            Contracts.AssertValue(value);

            Value = value;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            if (Value is ISymbolSlot slot)
            {
                return $"ResolvedObject({slot.DebugName()})";
            }
            return $"ResolvedObject({Value})";
        }
    }
}
