// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal abstract class BinaryOpDelegationStrategy : DelegationValidationStrategy, IOpDelegationStrategy
    {
        private readonly TexlFunction _function;

        public BinaryOpDelegationStrategy(BinaryOp op, TexlFunction function)
            : base(function)
        {
            Contracts.AssertValue(function);

            Op = op;
            _function = function;
        }

        public BinaryOp Op { get; }

        protected string FormatTelemetryMessage(string message)
        {
            Contracts.AssertNonEmpty(message);

            return string.Format(CultureInfo.InvariantCulture, "Op:{0}, {1}", Op, message);
        }

        public virtual bool IsOpSupportedByColumn(OperationCapabilityMetadata metadata, TexlNode column, DPath columnPath, TexlBinding binder)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(column);
            Contracts.AssertValue(binder);

            var result = metadata.IsBinaryOpInDelegationSupportedByColumn(Op, columnPath);
            if (!result)
            {
                // Filter(SharepointDS, BooleanCol And <some other predicate>), makes it non delegable since no column has And/Or capability
                if ((column.Kind == NodeKind.FirstName || column.Kind == NodeKind.DottedName) && (Op == BinaryOp.And || Op == BinaryOp.Or))
                {
                    if (binder.IsValidBooleanDelegableNode(column))
                    {
                        return IsOpSupportedByTable(metadata, column, binder);
                    }
                }
                
                TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage("Operator not supported by column."), column, binder);
                SuggestDelegationHint(column, binder, TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn, CharacterUtils.MakeSafeForFormatString(columnPath.ToString()));
            }

            return result;
        }

        public virtual bool IsOpSupportedByTable(OperationCapabilityMetadata metadata, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            if (!metadata.IsBinaryOpInDelegationSupported(Op))
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByClientSuggestionMessage_OpNotSupportedByClient, Op.ToString());
                return false;
            }

            if (!metadata.IsBinaryOpSupportedByTable(Op))
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Op.ToString());
                return false;
            }

            return true;
        }

        // Verifies if given kind of node is supported by function delegation.
        private bool IsSupportedNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding, IOpDelegationStrategy opDelStrategy, bool isRHSNode)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(opDelStrategy);

            if (!binding.IsRowScope(node))
            {
                // Check whether this is -
                //  1) in operator delegation and
                //  2) it is verifying if RHS node is supported and
                //  3) it is not an async node unless allowed by the AllowAsyncDelegation feature and
                //  4) it is a single column table and
                //  5) metadata belongs to cds datasource that supports delegation of CdsIn
                // If this check fails, verify if it is simply a valid node..
                // Eg of valid delegation functions -
                // Filter(Accounts, 'Account Name' in ["Foo", Bar"]) - Direct table use
                // Set(Names, ["Foo", Bar"]); Filter(Accounts, 'Account Name' in Names) - Using variable of type table
                // ClearCollect(Names, Accounts); Filter(Accounts, 'Account Name' in Names.'Account Name') - using column from collection.
                // This won't be delegated if the AllowAsyncDelegation- Filter(Accounts, 'Account Name' in Accounts.'Account Name') as Accounts.'Account Name' is async.
                if (isRHSNode && binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled
                    && (opDelStrategy as BinaryOpDelegationStrategy)?.Op == BinaryOp.In
                    && binding.GetType(node).IsTable 
                    && binding.GetType(node).IsColumn
                    && (IsValidAsyncOrImpureNode(node, binding) || !binding.IsAsync(node))
                    && opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                {
                    return true;
                }

                if (!binding.IsRowScope(node) && IsValidAsyncOrImpureNode(node, binding))
                {
                    return true;
                }
            }

            switch (node.Kind)
            {
                case NodeKind.DottedName:
                    {
                        if (!opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                        {
                            return false;
                        }

                        var dottedNodeValStrategy = _function.GetDottedNameNodeDelegationStrategy();
                        return dottedNodeValStrategy.IsValidDottedNameNode(node.AsDottedName(), binding, metadata, opDelStrategy);
                    }

                case NodeKind.Call:
                    {
                        if (!opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                        {
                            return false;
                        }

                        var cNodeValStrategy = _function.GetCallNodeDelegationStrategy();
                        return cNodeValStrategy.IsValidCallNode(node.AsCall(), binding, metadata);
                    }

                case NodeKind.FirstName:
                    {
                        var firstNameNodeValStrategy = _function.GetFirstNameNodeDelegationStrategy();
                        return firstNameNodeValStrategy.IsValidFirstNameNode(node.AsFirstName(), binding, opDelStrategy);
                    }

                case NodeKind.UnaryOp:
                    {
                        if (!opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                        {
                            return false;
                        }

                        var unaryopNode = node.AsUnaryOpLit();
                        var unaryOpNodeDelegationStrategy = _function.GetOpDelegationStrategy(unaryopNode.Op);
                        return unaryOpNodeDelegationStrategy.IsSupportedOpNode(unaryopNode, metadata, binding);
                    }

                case NodeKind.BinaryOp:
                    {
                        if (!opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                        {
                            return false;
                        }

                        var binaryOpNode = node.AsBinaryOp().VerifyValue();
                        opDelStrategy = _function.GetOpDelegationStrategy(binaryOpNode.Op, binaryOpNode);

                        var binaryOpDelStrategy = (opDelStrategy as BinaryOpDelegationStrategy).VerifyValue();
                        Contracts.Assert(binaryOpNode.Op == binaryOpDelStrategy.Op);

                        if (!opDelStrategy.IsSupportedOpNode(node, metadata, binding))
                        {
                            SuggestDelegationHint(binaryOpNode, binding);
                            return false;
                        }

                        break;
                    }

                default:
                    {
                        var kind = node.Kind;

                        if (kind != NodeKind.BoolLit && kind != NodeKind.StrLit && kind != NodeKind.NumLit)
                        {
                            var telemetryMessage = string.Format(CultureInfo.InvariantCulture, "NodeKind {0} unsupported.", kind);
                            SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
                            return false;
                        }

                        break;
                    }
            }

            return true;
        }

        private bool IsColumnNode(TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            return (node.Kind == NodeKind.FirstName) && binding.IsRowScope(node);
        }

        private bool DoCoercionCheck(BinaryOpNode binaryOpNode, OperationCapabilityMetadata metadata, TexlBinding binding)
        {
            Contracts.AssertValue(binaryOpNode);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);

            var leftType = binding.GetType(binaryOpNode.Left);
            var rightType = binding.GetType(binaryOpNode.Right);

            switch (leftType.Kind)
            {
                case DKind.Date:
                    if (rightType.Kind == DKind.DateTime)
                    {
                        // If rhs is a column of type DateTime and lhs is row scoped then we will need to apply the coercion on rhs. So check if coercion function date is supported or not.
                        if (IsColumnNode(binaryOpNode.Right, binding) && binding.IsRowScope(binaryOpNode.Left))
                        {
                            return IsDelegatableColumnNode(binaryOpNode.Right.AsFirstName(), binding, null, DelegationCapability.Date);
                        }

                        // If lhs is rowscoped but not a field reference and rhs is rowscoped then we need to check if it's supported at table level.
                        if (binding.IsRowScope(binaryOpNode.Left) && binding.IsRowScope(binaryOpNode.Right))
                        {
                            return metadata.IsDelegationSupportedByTable(DelegationCapability.Date);
                        }

                        return true;
                    }

                    break;
                case DKind.DateTime:
                    if (rightType.Kind == DKind.Date)
                    {
                        // If lhs is a column of type DateTime and RHS is also row scoped then check if coercion function date is supported or not.
                        if (IsColumnNode(binaryOpNode.Left, binding) && binding.IsRowScope(binaryOpNode.Right))
                        {
                            return IsDelegatableColumnNode(binaryOpNode.Left.AsFirstName(), binding, null, DelegationCapability.Date);
                        }

                        // If lhs is rowscoped but not a field reference and rhs is rowscoped then we need to check if it's supported at table level.
                        if (binding.IsRowScope(binaryOpNode.Left) && binding.IsRowScope(binaryOpNode.Right))
                        {
                            return metadata.IsDelegationSupportedByTable(DelegationCapability.Date);
                        }

                        return true;
                    }

                    break;
                default:
                    break;
            }

            return true;
        }

        public virtual bool IsSupportedOpNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);

            var binaryOpNode = node.AsBinaryOp();
            if (binaryOpNode == null)
            {
                return false;
            }

            var opDelStrategy = _function.GetOpDelegationStrategy(binaryOpNode.Op, binaryOpNode);
            var binaryOpDelStrategy = (opDelStrategy as BinaryOpDelegationStrategy).VerifyValue();
            Contracts.Assert(binaryOpNode.Op == binaryOpDelStrategy.Op);

            // Check if binaryOp is supported by datasource in the context of filter operation.
            // If this is not allowed then there is no point in checking lhs and rhs
            // It's only safe to do so if lhs and rhs is first/dotted name node as columns (FirstName/DottedName node) can have additional capabilities defined.
            if (!(binaryOpNode.Left is FirstNameNode || binaryOpNode.Left is DottedNameNode) &&
                !(binaryOpNode.Right is FirstNameNode || binaryOpNode.Right is DottedNameNode) &&
                !opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
            {
                var telemetryMessage = string.Format(CultureInfo.InvariantCulture, "{0} operator not supported at table level", binaryOpNode.Op.ToString());
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.BinaryOpNoSupported, node, binding, _function, DelegationTelemetryInfo.CreateBinaryOpNoSupportedInfoTelemetryInfo(binaryOpNode.Op));
                return false;
            }

            if (!ODataFunctionMappings.BinaryOpToOperatorMap.Value.ContainsKey(binaryOpNode.Op))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            if (!IsSupportedNode(binaryOpNode.Left, metadata, binding, opDelStrategy, false))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            if (!IsSupportedNode(binaryOpNode.Right, metadata, binding, opDelStrategy, true))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            var leftType = binding.GetType(binaryOpNode.Left);
            var rightType = binding.GetType(binaryOpNode.Right);
            if ((leftType.IsPolymorphic && rightType.IsRecord) || (leftType.IsRecord && rightType.IsPolymorphic))
            {
                return true;
            }

            if (!DoCoercionCheck(binaryOpNode, metadata, binding))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            return true;
        }
    }
}
