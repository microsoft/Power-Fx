// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    // Base class for all implementations of delegatable operation metadata.
    internal abstract class OperationCapabilityMetadata
    {
        public OperationCapabilityMetadata(DType schema)
        {
            Contracts.AssertValid(schema);

            Schema = schema;
        }

        protected virtual Dictionary<DPath, DelegationCapability> ColumnRestrictions => new Dictionary<DPath, DelegationCapability>();

        public virtual Dictionary<DPath, DPath> QueryPathReplacement => new Dictionary<DPath, DPath>();

        public abstract DelegationCapability DefaultColumnCapabilities { get; }

        public abstract DelegationCapability TableCapabilities { get; }

        protected bool TryGetColumnRestrictions(DPath columnPath, out DelegationCapability restrictions)
        {
            Contracts.AssertValid(columnPath);

            if (ColumnRestrictions.TryGetValue(columnPath, out restrictions))
            {
                return true;
            }

            restrictions = DelegationCapability.None;
            return false;
        }

        public DType Schema { get; }

        public virtual bool TryGetColumnCapabilities(DPath columnPath, out DelegationCapability capabilities)
        {
            Contracts.AssertValid(columnPath);

            // Check if it's a valid column name. As capabilities are not defined for all columns, we need to do this check.
            if (!Schema.TryGetType(columnPath, out _))
            {
                capabilities = DelegationCapability.None;
                return false;
            }

            capabilities = DefaultColumnCapabilities;

            if (TryGetColumnRestrictions(columnPath, out var restrictions))
            {
                capabilities &= ~restrictions;
            }

            return true;
        }

        public bool IsDelegationSupportedByColumn(DPath columnPath, DelegationCapability delegationCapability)
        {
            Contracts.AssertValid(columnPath);

            // Only the first part of the path can have been renamed
            if (DType.TryGetLogicalNameForColumn(Schema, columnPath[0], out var logicalName))
            {
                var renamedColumnPath = DPath.Root;
                renamedColumnPath = renamedColumnPath.Append(new DName(logicalName));
                for (var i = 1; i < columnPath.Length; ++i)
                {
                    renamedColumnPath = renamedColumnPath.Append(new DName(columnPath[i]));
                }

                columnPath = renamedColumnPath;
            }

            return TryGetColumnCapabilities(columnPath, out var columnCapabilities) && columnCapabilities.HasCapability(delegationCapability.Capabilities);
        }

        public virtual bool IsDelegationSupportedByTable(DelegationCapability delegationCapability)
        {
            return DefaultColumnCapabilities.HasCapability(delegationCapability.Capabilities);
        }

        public virtual bool IsUnaryOpSupportedByTable(UnaryOp op)
        {
            if (!IsUnaryOpInDelegationSupported(op))
            {
                return false;
            }

            Contracts.Assert(DelegationCapability.UnaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByTable(DelegationCapability.UnaryOpToDelegationCapabilityMap[op].Capabilities);
        }

        public virtual bool IsBinaryOpSupportedByTable(BinaryOp op)
        {
            if (!IsBinaryOpInDelegationSupported(op))
            {
                return false;
            }

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
            {
                return false;
            }

            Contracts.Assert(DelegationCapability.BinaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByColumn(columnPath, DelegationCapability.BinaryOpToDelegationCapabilityMap[op]);
        }

        public virtual bool IsUnaryOpInDelegationSupportedByColumn(UnaryOp op, DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            if (!IsUnaryOpInDelegationSupported(op))
            {
                return false;
            }

            Contracts.Assert(DelegationCapability.UnaryOpToDelegationCapabilityMap.ContainsKey(op));

            return IsDelegationSupportedByColumn(columnPath, DelegationCapability.UnaryOpToDelegationCapabilityMap[op]);
        }
    }
}
