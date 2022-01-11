using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    /// <summary>
    /// For Record->Record and Table->Table, provides coercions for individual fields, potentially recursively.
    /// </summary>
    internal sealed class AggregateCoercionNode : IntermediateNode
    {
        public readonly UnaryOpKind Op;
        public readonly IntermediateNode Child;
        public readonly IReadOnlyDictionary<DName, IntermediateNode> FieldCoercions;
        public readonly ScopeSymbol Scope;

        public AggregateCoercionNode(IRContext irContext, UnaryOpKind op, ScopeSymbol scope, IntermediateNode child, IReadOnlyDictionary<DName, IntermediateNode> fieldCoercions)
            : base(irContext)
        {
            Contracts.AssertValue(child);
            Contracts.Assert(op == UnaryOpKind.RecordToRecord || op == UnaryOpKind.TableToTable);

            Op = op;
            Scope = scope;
            Child = child;
            FieldCoercions = fieldCoercions;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"AggregateCoercion({Op}, ");
            sb.Append(string.Join(", ", FieldCoercions.Select(fc => $"{fc.Key.Value} <- {fc.Value}")));
            sb.Append(")");
            return sb.ToString();
        }
    }
}
