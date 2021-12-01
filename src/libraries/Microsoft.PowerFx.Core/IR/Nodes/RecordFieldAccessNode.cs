// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class RecordFieldAccessNode : IntermediateNode
    {
        public readonly IntermediateNode From;
        public readonly DName Field;

        public RecordFieldAccessNode(IRContext irContext, IntermediateNode from, DName field) : base(irContext)
        {
            Contracts.AssertValid(field);
            Contracts.AssertValue(from);

            From = from;
            Field = field;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }


        public override string ToString()
        {
            return $"FieldAccess({From}, {Field})";
        }
    }
}
