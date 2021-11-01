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

        /// <summary>
        /// For Record->Record and Table->Table, provides coercions for individual fields
        /// Null for other ops
        /// </summary>
        public readonly Dictionary<DName, CoercionKind> AggregateFieldCoercions;
        private readonly FormulaType AggregateCoercionResultType;

        public UnaryOpNode(IRContext irContext, UnaryOpKind op, IntermediateNode child) : base(irContext)
        {
            Contracts.AssertValue(child);

            Op = op;
            Child = child;
        }

        public UnaryOpNode(IRContext irContext, UnaryOpKind op, IntermediateNode child, Dictionary<DName, CoercionKind> fieldCoercions, FormulaType resultType) : this(irContext, op, child)
        {
            Contracts.AssertValue(resultType);
            Contracts.AssertValue(fieldCoercions);

            AggregateFieldCoercions = fieldCoercions;
            AggregateCoercionResultType = resultType;
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
