// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;

namespace Microsoft.PowerFx.Connectors
{
    internal class ODataVisitor : IRNodeVisitor<string, object>
    {
        internal static readonly ODataVisitor I = new ODataVisitor();

        public override string Visit(TextLiteralNode node, object context)
        {
            return $"'{node.LiteralValue}'";
        }

        public override string Visit(NumberLiteralNode node, object context)
        {
            return node.LiteralValue.ToString(CultureInfo.InvariantCulture);
        }

        public override string Visit(BooleanLiteralNode node, object context)
        {
            return node.LiteralValue.ToString(CultureInfo.InvariantCulture);
        }

        public override string Visit(ColorLiteralNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(RecordNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(ErrorNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(LazyEvalNode node, object context)
        {
            return node.Child.Accept(this, context);
        }

        public override string Visit(CallNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(BinaryOpNode node, object context)
        {
            return $"{node.Left.Accept(this, context)} {ODataOpFromBinaryOp(node.Op)} {node.Right.Accept(this, context)}";
        }

        public override string Visit(UnaryOpNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(ScopeAccessNode node, object context)
        {
            if (node.Value is ScopeAccessSymbol symbol)
            {
                return symbol.Name.Value;
            }

            return string.Empty;
        }

        public override string Visit(RecordFieldAccessNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(ResolvedObjectNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(SingleColumnTableAccessNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(ChainingNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        public override string Visit(AggregateCoercionNode node, object context)
        {
            throw new System.NotImplementedException();
        }

        private static string ODataOpFromBinaryOp(BinaryOpKind op) =>
            op switch
            {
                // BinaryOpKind.Invalid => expr,
                // BinaryOpKind.InText => expr,
                // BinaryOpKind.ExactInText => expr,
                // BinaryOpKind.InScalarTable => expr,
                // BinaryOpKind.ExactInScalarTable => expr,
                // BinaryOpKind.InRecordTable => expr,
                BinaryOpKind.AddNumbers => "+",
                BinaryOpKind.AddDateAndTime => "+",
                BinaryOpKind.AddDateAndDay => "+",
                BinaryOpKind.AddDateTimeAndDay => "+",
                BinaryOpKind.AddTimeAndMilliseconds => "+",
                BinaryOpKind.DateDifference => "-",
                BinaryOpKind.TimeDifference => "-",
                
                // BinaryOpKind.MulNumbers => expr,
                BinaryOpKind.DivNumbers => "/",
                BinaryOpKind.EqNumbers => "eq",
                BinaryOpKind.EqBoolean => "eq",
                BinaryOpKind.EqText => "eq",
                BinaryOpKind.EqDate => "eq",
                BinaryOpKind.EqTime => "eq",
                BinaryOpKind.EqDateTime => "eq",
                BinaryOpKind.EqHyperlink => "eq",
                BinaryOpKind.EqCurrency => "eq",
                BinaryOpKind.EqImage => "eq",
                BinaryOpKind.EqColor => "eq",
                BinaryOpKind.EqMedia => "eq",
                BinaryOpKind.EqBlob => "eq",
                BinaryOpKind.EqGuid => "eq",
                BinaryOpKind.EqOptionSetValue => "eq",
                BinaryOpKind.EqViewValue => "eq",
                BinaryOpKind.EqNamedValue => "eq",
                BinaryOpKind.EqNull => "eq",
                BinaryOpKind.NeqNumbers => "ne",
                BinaryOpKind.NeqBoolean => "ne",
                BinaryOpKind.NeqText => "ne",
                BinaryOpKind.NeqDate => "ne",
                BinaryOpKind.NeqTime => "ne",
                BinaryOpKind.NeqDateTime => "ne",
                BinaryOpKind.NeqHyperlink => "ne",
                BinaryOpKind.NeqCurrency => "ne",
                BinaryOpKind.NeqImage => "ne",
                BinaryOpKind.NeqColor => "ne",
                BinaryOpKind.NeqMedia => "ne",
                BinaryOpKind.NeqBlob => "ne",
                BinaryOpKind.NeqGuid => "ne",
                BinaryOpKind.NeqOptionSetValue => "ne",
                BinaryOpKind.NeqViewValue => "ne",
                BinaryOpKind.NeqNamedValue => "ne",
                BinaryOpKind.NeqNull => "ne",
                BinaryOpKind.LtNumbers => "lt",
                BinaryOpKind.LeqNumbers => "le",
                BinaryOpKind.GtNumbers => "gt",
                BinaryOpKind.GeqNumbers => "ge",
                BinaryOpKind.LtDateTime => "lt",
                BinaryOpKind.LeqDateTime => "le",
                BinaryOpKind.GtDateTime => "gt",
                BinaryOpKind.GeqDateTime => "ge",
                BinaryOpKind.LtDate => "lt",
                BinaryOpKind.LeqDate => "le",
                BinaryOpKind.GtDate => "gt",
                BinaryOpKind.GeqDate => "ge",
                BinaryOpKind.LtTime => "lt",
                BinaryOpKind.LeqTime => "le",
                BinaryOpKind.GtTime => "gt",
                BinaryOpKind.GeqTime => "ge",
                
                // BinaryOpKind.DynamicGetField => expr,
                // BinaryOpKind.Power => expr,
                // BinaryOpKind.Concatenate => expr,
                BinaryOpKind.And => "and",
                BinaryOpKind.Or => "or",
                
                // BinaryOpKind.AddTimeAndDate => expr,
                // BinaryOpKind.AddDayAndDate => expr,
                // BinaryOpKind.AddMillisecondsAndTime => expr,
                // BinaryOpKind.AddDayAndDateTime => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
    }
}
