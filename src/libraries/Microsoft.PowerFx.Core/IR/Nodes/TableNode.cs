// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class TableNode : IntermediateNode
    {
        public readonly IReadOnlyList<IntermediateNode> Values;

        public TableNode(IRContext irContext, params IntermediateNode[] values)
            : base(irContext)
        {
            Contracts.AssertAllValues(values);

            Values = values;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"Table({string.Join(",", Values)})";
        }
    }
}
