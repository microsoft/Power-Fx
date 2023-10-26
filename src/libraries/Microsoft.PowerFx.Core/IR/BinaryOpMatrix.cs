﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.IR
{
    internal class BinaryOpMatrix
    {
        public static BinaryOpKind GetBinaryOpKind(PowerFx.Syntax.BinaryOpNode node, TexlBinding binding)
        {
            var parsedBinaryOp = node.Op;
            var leftType = binding.GetType(node.Left);
            var rightType = binding.GetType(node.Right);

            return parsedBinaryOp switch
            {
                BinaryOp.In or BinaryOp.Exactin => GetInOp(binding, parsedBinaryOp, leftType, rightType),
                BinaryOp.Power => BinaryOpKind.Power,
                BinaryOp.Concat => BinaryOpKind.Concatenate,
                BinaryOp.And => BinaryOpKind.And,
                BinaryOp.Or => BinaryOpKind.Or,
                BinaryOp.Add => GetAddOp(node, binding, leftType, rightType),
                BinaryOp.Mul => binding.GetType(node) == DType.Decimal ? BinaryOpKind.MulDecimals : BinaryOpKind.MulNumbers,
                BinaryOp.Div => binding.GetType(node) == DType.Decimal ? BinaryOpKind.DivDecimals : BinaryOpKind.DivNumbers,
                BinaryOp.Equal or
                BinaryOp.NotEqual or
                BinaryOp.Less or
                BinaryOp.Greater or
                BinaryOp.LessEqual or
                BinaryOp.GreaterEqual => GetBooleanBinaryOp(node, binding, leftType, rightType),
                BinaryOp.Error => BinaryOpKind.Invalid,
                _ => throw new NotSupportedException(),
            };
        }

        private static BinaryOpKind GetBooleanBinaryOp(PowerFx.Syntax.BinaryOpNode node, TexlBinding binding, DType leftType, DType rightType)
        {
            // Check untyped object special case first
            if ((leftType.IsUntypedObject && rightType.Kind == DKind.ObjNull) ||
                (rightType.IsUntypedObject && leftType.Kind == DKind.ObjNull))
            {
                switch (node.Op)
                {
                    case BinaryOp.NotEqual:
                        return BinaryOpKind.NeqNullUntyped;
                    case BinaryOp.Equal:
                        return BinaryOpKind.EqNullUntyped;
                }
            }

            var kindToUse = leftType.Accepts(rightType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: binding.Features.PowerFxV1CompatibilityRules) ? leftType.Kind : rightType.Kind;

            // If there is coercion involved, pick the coerced type.
            if (binding.TryGetCoercedType(node.Left, out var leftCoerced))
            {
                kindToUse = leftCoerced.Kind;
            }
            else if (binding.TryGetCoercedType(node.Right, out var rightCoerced))
            {
                kindToUse = rightCoerced.Kind;
            }
            else if (!leftType.Accepts(rightType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: binding.Features.PowerFxV1CompatibilityRules) &&
                !rightType.Accepts(leftType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: binding.Features.PowerFxV1CompatibilityRules))
            {
                return BinaryOpKind.Invalid;
            } 
            
            switch (kindToUse)
            {
                case DKind.Number:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqNumbers;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqNumbers;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtNumbers;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqNumbers;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtNumbers;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqNumbers;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Decimal:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqDecimals;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqDecimals;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtDecimals;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqDecimals;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtDecimals;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqDecimals;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Date:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqDate;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqDate;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtDate;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqDate;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtDate;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqDate;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.DateTime:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqDateTime;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqDateTime;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtDateTime;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqDateTime;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtDateTime;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqDateTime;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Time:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqTime;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqTime;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtTime;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqTime;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtTime;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqTime;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Boolean:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqBoolean;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqBoolean;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.String:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqText;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqText;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Hyperlink:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqHyperlink;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqHyperlink;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Currency:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqCurrency;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqCurrency;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Image:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqImage;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqImage;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Color:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqColor;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqColor;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Media:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqMedia;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqMedia;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Blob:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqBlob;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqBlob;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Guid:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqGuid;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqGuid;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.ObjNull:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqNull;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqNull;
                        case BinaryOp.Less:
                            return BinaryOpKind.LtNull;
                        case BinaryOp.LessEqual:
                            return BinaryOpKind.LeqNull;
                        case BinaryOp.Greater:
                            return BinaryOpKind.GtNull;
                        case BinaryOp.GreaterEqual:
                            return BinaryOpKind.GeqNull;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.OptionSetValue:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqOptionSetValue;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqOptionSetValue;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.ViewValue:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqViewValue;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqViewValue;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.NamedValue:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqNamedValue;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqNamedValue;
                        default:
                            throw new NotSupportedException();
                    }

                case DKind.Polymorphic:
                    switch (node.Op)
                    {
                        case BinaryOp.NotEqual:
                            return BinaryOpKind.NeqPolymorphic;
                        case BinaryOp.Equal:
                            return BinaryOpKind.EqPolymorphic;
                        default: 
                            throw new NotSupportedException();
                    }

                default:
                    throw new NotSupportedException("Not supported comparison op on type " + kindToUse.ToString());
            }
        }

        private static BinaryOpKind GetAddOp(PowerFx.Syntax.BinaryOpNode node, TexlBinding binding, DType leftType, DType rightType)
        {
            switch (leftType.Kind)
            {
                case DKind.Date:
                    if (rightType == DType.DateTime || rightType == DType.Date)
                    {
                        // Date + '-DateTime' => in days
                        // Date + '-Date' => in days

                        // Binding produces this as '-Date'. This should be cleaned up when we switch to a proper sub op. 
                        if (node.Right is not PowerFx.Syntax.UnaryOpNode { Op: UnaryOp.Minus })
                        {
                            throw new NotSupportedException();
                        }

                        return BinaryOpKind.DateDifference;
                    }
                    else if (rightType == DType.Time)
                    {
                        return BinaryOpKind.AddDateAndTime;
                    }
                    else
                    {
                        return BinaryOpKind.AddDateAndDay;
                    }

                case DKind.Time:
                    if (rightType == DType.Date || rightType == DType.DateTime)
                    {
                        // Time + Date => DateTime
                        return BinaryOpKind.AddTimeAndDate;
                    }
                    else if (rightType == DType.Time)
                    {
                        if (node.Right.AsUnaryOpLit()?.Op == UnaryOp.Minus)
                        {
                            // Time + '-Time' => in ms
                            return BinaryOpKind.TimeDifference;
                        }
                        else
                        {
                            // Time + Time => Time
                            return BinaryOpKind.AddTimeAndTime;
                        }
                    }
                    else
                    {
                        // Time + Number
                        return BinaryOpKind.AddTimeAndNumber;
                    }

                case DKind.DateTime:
                    if (rightType == DType.DateTime || rightType == DType.Date)
                    {
                        // DateTime + '-DateTime' => in days
                        // DateTime + '-Date' => in days

                        // Ensure that this is really '-Date' - Binding should always catch this, but let's make sure...
                        Contracts.Assert(node.Right.AsUnaryOpLit().VerifyValue().Op == UnaryOp.Minus);
                        return BinaryOpKind.DateDifference;
                    }
                    else if (rightType == DType.Time)
                    {
                        return BinaryOpKind.AddDateAndTime;
                    }
                    else
                    {
                        return BinaryOpKind.AddDateTimeAndDay;
                    }

                default:
                    switch (rightType.Kind)
                    {
                        // Operations with Date/DateTime/Time and Decimal promote the Decimal to float
                        case DKind.Date:
                            if (node.Right.AsUnaryOpLit()?.Op == UnaryOp.Minus)
                            {
                                // Number + '-Date'
                                return BinaryOpKind.SubtractNumberAndDate;
                            }
                            else
                            {
                                // Number + Date
                                return BinaryOpKind.AddDayAndDate;
                            }

                        case DKind.Time:
                            if (node.Right.AsUnaryOpLit()?.Op == UnaryOp.Minus)
                            {
                                // Number + '-DateTime'
                                return BinaryOpKind.SubtractNumberAndTime;
                            }
                            else
                            {
                                // Number + Time
                                return BinaryOpKind.AddNumberAndTime;
                            }

                        case DKind.DateTime:
                            if (node.Right.AsUnaryOpLit()?.Op == UnaryOp.Minus)
                            {
                                // Number + '-DateTime'
                                return BinaryOpKind.SubtractNumberAndDateTime;
                            }
                            else
                            {
                                // Number + Date
                                return BinaryOpKind.AddDayAndDateTime;
                            }

                        default:
                            if (binding.GetType(node) == DType.Decimal)
                            {
                                // Decimal + Decimal
                                return BinaryOpKind.AddDecimals;
                            }
                            else
                            {
                                // Number + Number
                                return BinaryOpKind.AddNumbers;
                            }
                    }
            }
        }

        private static BinaryOpKind GetInOp(TexlBinding binding, BinaryOp parsedBinaryOp, DType leftType, DType rightType)
        {
            var usesPFxV1CompatRules = binding.Features.PowerFxV1CompatibilityRules;
            if (!rightType.IsAggregate || (usesPFxV1CompatRules && rightType.Kind == DKind.ObjNull))
            {
                if ((DType.String.Accepts(rightType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usesPFxV1CompatRules) && (DType.String.Accepts(leftType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usesPFxV1CompatRules) || leftType.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, binding.Features))) ||
                    (rightType.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, binding.Features) && DType.String.Accepts(leftType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usesPFxV1CompatRules)))
                {
                    return parsedBinaryOp == BinaryOp.In ? BinaryOpKind.InText : BinaryOpKind.ExactInText;
                }

                return BinaryOpKind.Invalid;
            }

            if (!leftType.IsAggregate || (usesPFxV1CompatRules && leftType.Kind == DKind.ObjNull))
            {
                if (rightType.IsTable)
                {
                    // scalar in table: in_ST(left, right)
                    // scalar exactin table: exactin_ST(left, right)
                    return parsedBinaryOp == BinaryOp.In ? BinaryOpKind.InScalarTable : BinaryOpKind.ExactInScalarTable;
                }

                // scalar in record: not supported
                // scalar exactin record: not supported
                return BinaryOpKind.Invalid;
            }

            if (leftType.IsRecord)
            {
                if (rightType.IsTable)
                {
                    // record in table: in_RT(left, right)
                    // record exactin table: in_RT(left, right)
                    // This is done regardless of "exactness".
                    return BinaryOpKind.InRecordTable;
                }

                // record in record: not supported
                // record exactin record: not supported
                return BinaryOpKind.Invalid;
            }

            // table in anything: not supported
            // table exactin anything: not supported
            return BinaryOpKind.Invalid;
        }
    }
}
