// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class ODataVisitor : IRNodeVisitor<string, ODataVisitorContext>
    {
        internal static readonly ODataVisitor I = new ODataVisitor();

        public override string Visit(TextLiteralNode node, ODataVisitorContext runContext)
        {
            return SerializeStringValue(node.LiteralValue);
        }

        public override string Visit(NumberLiteralNode node, ODataVisitorContext runContext)
        {
            return SerializeNumberValue(node.LiteralValue);
        }

        public override string Visit(BooleanLiteralNode node, ODataVisitorContext runContext)
        {
            return SerializeBooleanValue(node.LiteralValue);
        }

        public override string Visit(ColorLiteralNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(RecordNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(ErrorNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(LazyEvalNode node, ODataVisitorContext runContext)
        {
            return node.Child.Accept(this, runContext);
        }

        public override string Visit(CallNode node, ODataVisitorContext runContext)
        {
            // Special cases for functions supported by OData
            switch (node.Function)
            {
                case NotFunction:
                    return NotOp(node.Args[0], runContext);
                case StartsWithFunction:
                    return StartsWithOp(node.Args[0], node.Args[1], runContext);
                case EndsWithFunction:
                    return EndsWithOp(node.Args[0], node.Args[1], runContext);
            }

            // Fallback: try to evaluate call node, fail if it tries to access a delegated scope
            try
            {
                FormulaValue result = runContext.EvalAsync(node).Result;
                return SerializeLiteralValue(result);
            }
            catch (KeyNotFoundException)
            {
                // scope access to table value
                throw new NotDelegableException();
            }
        }

        public override string Visit(BinaryOpNode node, ODataVisitorContext runContext)
        {
            try
            {
                FormulaValue result = runContext.EvalAsync(node).Result;
                return SerializeLiteralValue(result);
            }
            catch (KeyNotFoundException)
            {
                return $"{node.Left.Accept(this, runContext)} {ODataOpFromBinaryOp(node.Op)} {node.Right.Accept(this, runContext)}";
            }
        }

        public override string Visit(UnaryOpNode node, ODataVisitorContext runContext)
        {
            return node.Op switch
            {
                UnaryOpKind.Negate => NotOp(node.Child, runContext),
                _ => throw new NotDelegableException(),
            };
        }

        public override string Visit(ScopeAccessNode node, ODataVisitorContext runContext)
        {
            if (node.Value is ScopeAccessSymbol symbol)
            {
                return symbol.Name.Value;
            }

            return string.Empty;
        }

        public override string Visit(RecordFieldAccessNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(ResolvedObjectNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(SingleColumnTableAccessNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(ChainingNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        public override string Visit(AggregateCoercionNode node, ODataVisitorContext runContext)
        {
            throw new NotDelegableException();
        }

        private string NotOp(IntermediateNode node, ODataVisitorContext runContext)
        {
            return $"not({node.Accept(this, runContext)})";
        }

        private string StartsWithOp(IntermediateNode nodeA, IntermediateNode nodeB, ODataVisitorContext runContext)
        {
            return $"startswith({nodeA.Accept(this, runContext)}, {nodeB.Accept(this, runContext)})";
        }

        private string EndsWithOp(IntermediateNode nodeA, IntermediateNode nodeB, ODataVisitorContext runContext)
        {
            return $"endswith({nodeA.Accept(this, runContext)}, {nodeB.Accept(this, runContext)})";
        }

        private static string SerializeLiteralValue(FormulaValue value)
        {
            return value switch
            {
                BooleanValue booleanValue => SerializeBooleanValue(booleanValue.Value),
                ColorValue colorValue => throw new NotDelegableException(),
                DateTimeValue dateTimeValue => dateTimeValue.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                DateValue dateValue => dateValue.Value.ToString("yyyy-MM-dd"),
                GuidValue guidValue => guidValue.Value.ToString(),
                NumberValue numberValue => SerializeNumberValue(numberValue.Value),
                StringValue stringValue => SerializeStringValue(stringValue.Value),
                TimeValue timeValue => throw new NotDelegableException(),
                _ => throw new NotDelegableException(),
            };
        }

        private static string SerializeNumberValue(double value) => value.ToString(CultureInfo.InvariantCulture);

        private static string SerializeStringValue(string value) => '\'' + value.Replace("'", "''") + '\'';

        private static string SerializeBooleanValue(bool value) => value ? "true" : "false";

        private static string ODataOpFromBinaryOp(BinaryOpKind op) =>
            op switch
            {
                BinaryOpKind.AddNumbers => "+",
                BinaryOpKind.AddDateAndTime => "+",
                BinaryOpKind.AddDateAndDay => "+",
                BinaryOpKind.AddDateTimeAndDay => "+",
                BinaryOpKind.AddTimeAndNumber => "+",
                BinaryOpKind.DateDifference => "-",
                BinaryOpKind.TimeDifference => "-",
                BinaryOpKind.SubtractDateAndTime => "-",
                BinaryOpKind.SubtractNumberAndDate => "-",
                BinaryOpKind.SubtractNumberAndDateTime => "-",
                BinaryOpKind.SubtractNumberAndTime => "-",
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

                BinaryOpKind.And => "and",
                BinaryOpKind.Or => "or",

                // BinaryOpKind.DynamicGetField => expr,
                // BinaryOpKind.Power => expr,
                // BinaryOpKind.Concatenate => expr,
                // BinaryOpKind.AddTimeAndDate => expr,
                // BinaryOpKind.AddDayAndDate => expr,
                // BinaryOpKind.AddMillisecondsAndTime => expr,
                // BinaryOpKind.AddDayAndDateTime => expr,
                // BinaryOpKind.InText => expr,
                // BinaryOpKind.ExactInText => expr,
                // BinaryOpKind.InScalarTable => expr,
                // BinaryOpKind.ExactInScalarTable => expr,
                // BinaryOpKind.InRecordTable => expr,
                // BinaryOpKind.MulNumbers => expr,
                _ => throw new NotDelegableException(),
            };
    }
}
