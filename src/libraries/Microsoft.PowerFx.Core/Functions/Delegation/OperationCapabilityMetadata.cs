// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    // Operator strings which are part of delegation metadata Json.
    // These are used when parsing the metadata Json.
    internal static class DelegationMetadataOperatorConstants
    {
        public const string Equal = "eq";
        public const string NotEqual = "ne";
        public const string Less = "lt";
        public const string LessEqual = "le";
        public const string Greater = "gt";
        public const string GreaterEqual = "ge";
        public const string And = "and";
        public const string Or = "or";
        public const string Contains = "contains";
        public const string IndexOf = "indexof";
        public const string SubStringOf = "substringof";
        public const string Not = "not";
        public const string Year = "year";
        public const string Month = "month";
        public const string Day = "day";
        public const string Hour = "hour";
        public const string Minute = "minute";
        public const string Second = "second";
        public const string Lower = "tolower";
        public const string Upper = "toupper";
        public const string Trim = "trim";
        public const string Null = "null";
        public const string Date = "date";
        public const string Length = "length";
        public const string Sum = "sum";
        public const string Min = "min";
        public const string Max = "max";
        public const string Average = "average";
        public const string Count = "count";
        public const string Add = "add";
        public const string Sub = "sub";
        public const string StartsWith = "startswith";
        public const string Mul = "mul";
        public const string Div = "div";
        public const string EndsWith = "endswith";
        public const string CountDistinct = "countdistinct";
        public const string CdsIn = "cdsin";
        public const string Top = "top";
        public const string AsType = "astype";
        public const string ArrayLookup = "arraylookup";
    }

    // Base class for all implementations of delegatable operation metadata.
    internal abstract class OperationCapabilityMetadata
    {
        private readonly DType _tableSchema;

        public OperationCapabilityMetadata(DType schema)
        {
            Contracts.AssertValid(schema);

            _tableSchema = schema;
        }

        protected virtual Dictionary<DPath, DelegationCapability> ColumnRestrictions { get { return new Dictionary<DPath, DelegationCapability>(); } }

        public virtual Dictionary<DPath, DPath> QueryPathReplacement { get { return new Dictionary<DPath, DPath>(); } }

        public abstract DelegationCapability DefaultColumnCapabilities { get; }

        public abstract DelegationCapability TableCapabilities { get; }

        protected bool TryGetColumnRestrictions(DPath columnPath, out DelegationCapability restrictions)
        {
            Contracts.AssertValid(columnPath);

            if (ColumnRestrictions.TryGetValue(columnPath, out restrictions))
                return true;

            restrictions = DelegationCapability.None;
            return false;
        }

        public DType Schema { get { return _tableSchema; } }

        public virtual bool TryGetColumnCapabilities(DPath columnPath, out DelegationCapability capabilities)
        {
            Contracts.AssertValid(columnPath);

            // Check if it's a valid column name. As capabilities are not defined for all columns, we need to do this check.
            DType _;
            if (!_tableSchema.TryGetType(columnPath, out _))
            {
                capabilities = DelegationCapability.None;
                return false;
            }

            capabilities = DefaultColumnCapabilities;

            DelegationCapability restrictions;
            if (TryGetColumnRestrictions(columnPath, out restrictions))
                capabilities &= ~restrictions;

            return true;
        }

        public bool IsDelegationSupportedByColumn(DPath columnPath, DelegationCapability delegationCapability)
        {
            Contracts.AssertValid(columnPath);

            // Only the first part of the path can have been renamed
            string logicalName;
            if (DType.TryGetLogicalNameForColumn(_tableSchema, columnPath[0], out logicalName))
            {
                var renamedColumnPath = DPath.Root;
                renamedColumnPath = renamedColumnPath.Append(new DName(logicalName));
                for (int i = 1; i < columnPath.Length; ++i)
                    renamedColumnPath = renamedColumnPath.Append(new DName(columnPath[i]));

                columnPath = renamedColumnPath;
            }

            DelegationCapability columnCapabilities;
            return TryGetColumnCapabilities(columnPath, out columnCapabilities) && columnCapabilities.HasCapability(delegationCapability.Capabilities);
        }

        public virtual bool IsDelegationSupportedByTable(DelegationCapability delegationCapability)
        {
            return DefaultColumnCapabilities.HasCapability(delegationCapability.Capabilities);
        }

        public virtual bool IsUnaryOpSupportedByTable(UnaryOp op)
        {
            if (!IsUnaryOpInDelegationSupported(op))
                return false;

            Contracts.Assert(DelegationCapability.UnaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByTable(DelegationCapability.UnaryOpToDelegationCapabilityMap[op].Capabilities);
        }

        public virtual bool IsBinaryOpSupportedByTable(BinaryOp op)
        {
            if (!IsBinaryOpInDelegationSupported(op))
                return false;

            Contracts.Assert(DelegationCapability.BinaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByTable(DelegationCapability.BinaryOpToDelegationCapabilityMap[op].Capabilities);
        }

        public virtual bool IsUnaryOpInDelegationSupported(UnaryOp op)
        {
            // Check if unary op is supported
            switch (op)
            {
            case UnaryOp.Not:
            case UnaryOp.Minus:
                break;
            default:
                return false;
            }

            return true;
        }

        public virtual bool IsBinaryOpInDelegationSupported(BinaryOp op)
        {
            // Check if binary op is supported
            switch (op)
            {
            case BinaryOp.Equal:
            case BinaryOp.NotEqual:
            case BinaryOp.Less:
            case BinaryOp.LessEqual:
            case BinaryOp.Greater:
            case BinaryOp.GreaterEqual:
            case BinaryOp.And:
            case BinaryOp.Or:
            case BinaryOp.In:
            case BinaryOp.Add:
            case BinaryOp.Mul:
            case BinaryOp.Div:
                break;
            default:
                return false;
            }

            return true;
        }

        public virtual bool IsBinaryOpInDelegationSupportedByColumn(BinaryOp op, DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            if (!IsBinaryOpInDelegationSupported(op))
                return false;

            Contracts.Assert(DelegationCapability.BinaryOpToDelegationCapabilityMap.ContainsKey(op));


            return IsDelegationSupportedByColumn(columnPath, DelegationCapability.BinaryOpToDelegationCapabilityMap[op]);
        }

        public virtual bool IsUnaryOpInDelegationSupportedByColumn(UnaryOp op, DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            if (!IsUnaryOpInDelegationSupported(op))
                return false;

            Contracts.Assert(DelegationCapability.UnaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByColumn(columnPath, DelegationCapability.UnaryOpToDelegationCapabilityMap[op]);
        }
    }
}
