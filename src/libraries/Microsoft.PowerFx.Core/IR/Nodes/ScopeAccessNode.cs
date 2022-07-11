// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ScopeAccessNode : IntermediateNode
    {
        /// <summary>
        /// Either a ScopeSymbol or a ScopeAccessSymbol
        /// A ScopeSymbol here represents access to the whole scope record,
        /// A ScopeAccessSymbol here represents access to a single field from the scope.
        /// </summary>
        public IScopeSymbol Value;

        public ScopeAccessNode(IRContext irContext, IScopeSymbol symbol)
            : base(irContext)
        {
            Contracts.AssertValue(symbol);

            Value = symbol;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context, StackMarker stackMarker)
        {
            return visitor.Visit(this, context, stackMarker);
        }

        public override string ToString()
        {
            return $"ScopeAccess({Value})";
        }
    }
}
