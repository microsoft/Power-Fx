// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class UnitsLiteralNode : IntermediateNode
    {
        public readonly UnitInfo UnitInfo;

        public UnitsLiteralNode(IRContext irContext, UnitInfo unitInfo)
            : base(irContext)
        {
            UnitInfo = unitInfo;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"{UnitInfo}:U";
        }
    }
}
