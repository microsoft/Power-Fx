// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal abstract class UnaryOpDelegationStrategy : DelegationValidationStrategy, IOpDelegationStrategy
    {
        private readonly TexlFunction _function;

        public UnaryOpDelegationStrategy(UnaryOp op, TexlFunction function)
            : base(function)
        {
            Contracts.AssertValue(function);

            Op = op;
            _function = function;
        }

        public UnaryOp Op { get; }

        protected string FormatTelemetryMessage(string message)
        {
            Contracts.AssertNonEmpty(message);

            return string.Format("Op:{0}, {1}", Op, message);
        }

        public virtual bool IsOpSupportedByColumn(OperationCapabilityMetadata metadata, TexlNode column, DPath columnPath, TexlBinding binder)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(column);
            Contracts.AssertValue(binder);

            var result = metadata.IsUnaryOpInDelegationSupportedByColumn(Op, columnPath);
            if (!result)
            {
                TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage("Operator not supported by column."), column, binder);
            }

            return result;
        }

        public virtual bool IsOpSupportedByTable(OperationCapabilityMetadata metadata, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            if (!metadata.IsUnaryOpInDelegationSupported(Op))
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByClientSuggestionMessage_OpNotSupportedByClient, Op.ToString());
                return false;
            }

            if (!metadata.IsUnaryOpSupportedByTable(Op))
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Op.ToString());
                return false;
            }

            return true;
        }

        private bool IsSupportedNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding, IOpDelegationStrategy opDelStrategy)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(opDelStrategy);

            if (!binding.IsRowScope(node))
            {
                return true;
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

                        var unaryOpNode = node.AsUnaryOpLit().VerifyValue();
                        opDelStrategy = _function.GetOpDelegationStrategy(unaryOpNode.Op).VerifyValue();

                        var unaryOpDelStrategy = (opDelStrategy as UnaryOpDelegationStrategy).VerifyValue();
                        Contracts.Assert(unaryOpDelStrategy.Op == unaryOpNode.Op);

                        if (!opDelStrategy.IsSupportedOpNode(node, metadata, binding))
                        {
                            SuggestDelegationHint(node, binding);
                            return false;
                        }

                        return true;
                    }

                case NodeKind.BinaryOp:
                    {
                        if (!opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
                        {
                            return false;
                        }

                        var binaryOpNode = node.AsBinaryOp().VerifyValue();
                        var binaryOpNodeDelValidationStrategy = _function.GetOpDelegationStrategy(binaryOpNode.Op, binaryOpNode);
                        return binaryOpNodeDelValidationStrategy.IsSupportedOpNode(node.AsBinaryOp(), metadata, binding);
                    }
            }

            SuggestDelegationHint(node, binding);
            return false;
        }

        public virtual bool IsSupportedOpNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);

            var unaryOpNode = node.AsUnaryOpLit();
            if (unaryOpNode == null)
            {
                return false;
            }

            if (!IsValidNode(node, binding))
            {
                return false;
            }

            var opDelStrategy = _function.GetOpDelegationStrategy(unaryOpNode.Op);
            var unaryOpDelStrategy = (opDelStrategy as UnaryOpDelegationStrategy).VerifyValue();
            Contracts.Assert(unaryOpDelStrategy.Op == unaryOpNode.Op);

            if ((unaryOpNode.Child.Kind != NodeKind.FirstName) && !opDelStrategy.IsOpSupportedByTable(metadata, node, binding))
            {
                var telemetryMessage = string.Format("{0} operator not supported at table level", unaryOpNode.Op.ToString());
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.UnaryOpNotSupportedByTable, node, binding, _function, DelegationTelemetryInfo.CreateUnaryOpNoSupportedInfoTelemetryInfo(unaryOpNode.Op));
                return false;
            }

            return IsSupportedNode(unaryOpNode.Child, metadata, binding, opDelStrategy);
        }
    }
}
