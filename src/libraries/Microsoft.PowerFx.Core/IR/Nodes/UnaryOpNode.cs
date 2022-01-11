// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class UnaryOpNode : IntermediateNode
    {
        public readonly UnaryOpKind Op;
        public readonly IntermediateNode Child;

        public UnaryOpNode(IRContext irContext, UnaryOpKind op, IntermediateNode child)
            : base(irContext)
        {
            Contracts.AssertValue(child);
            Contracts.Assert(op != UnaryOpKind.RecordToRecord && op != UnaryOpKind.TableToTable);

            Op = op;
            Child = child;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"UnaryOp({Op}, {Child})";
        }
    }
}
