// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal sealed class InOpDelegationStrategy : BinaryOpDelegationStrategy
    {
        private readonly BinaryOpNode _binaryOpNode;

        public InOpDelegationStrategy(BinaryOpNode node, TexlFunction function)
            : base(BinaryOp.In, function)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Op == BinaryOp.In);

            _binaryOpNode = node;
        }

        public override bool IsSupportedOpNode(TexlNode node, OperationCapabilityMetadata metadata, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);

            var binaryOpNode = node?.AsBinaryOp();
            if (binaryOpNode == null)
            {
                return false;
            }

            var isRHSDelegableTable = IsRHSDelegableTable(binding, binaryOpNode, metadata);
            if (isRHSDelegableTable && binaryOpNode.Left is DottedNameNode dottedField && binding.GetType(dottedField.Left).HasExpandInfo)
            {
                return base.IsSupportedOpNode(node, metadata, binding);
            }

            DName columnName = default;
            FirstNameInfo info = null;

            var isFullyQualifiedFieldAccess = CheckForFullyQualifiedFieldAccess(isRHSDelegableTable, binaryOpNode, binding, node, ref columnName, ref info);
            if (!isFullyQualifiedFieldAccess)
            {
                return false;
            }

            var isRowScopedOrLambda = IsRowScopedOrLambda(binding, node, info, columnName, metadata);
            if (!isRowScopedOrLambda)
            {
                return false;
            }

            var leftType = binding.GetType(binaryOpNode.Left);
            var rightType = binding.GetType(binaryOpNode.Right);
            var isLeftNodeAsync = binding.IsAsync(binaryOpNode.Left);
            var isRightNodeAsync = binding.IsAsync(binaryOpNode.Right);
            if ((leftType.IsMultiSelectOptionSet() && isRightNodeAsync) || (rightType.IsMultiSelectOptionSet() && isLeftNodeAsync))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            return base.IsSupportedOpNode(node, metadata, binding);
        }

        public bool IsRHSDelegableTable(TexlBinding binding, BinaryOpNode binaryOpNode, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(binaryOpNode);
            Contracts.AssertValue(metadata);

            var rightNodeType = binding.GetType(binaryOpNode.Right);

            var hasEnhancedDelegation = binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled;
            var isColumn = rightNodeType?.IsColumn == true;
            var isDelegationSupportedByTable = metadata.IsDelegationSupportedByTable(DelegationCapability.CdsIn);
            var hasLeftFirstNameNodeOrIsFullRecordRowScopeAccessOrLookup = binaryOpNode.Left?.AsFirstName() != null || binding.IsFullRecordRowScopeAccess(binaryOpNode.Left) || (binaryOpNode.Left is DottedNameNode dottedField && binding.GetType(dottedField.Left).HasExpandInfo);

            return hasEnhancedDelegation && isColumn && isDelegationSupportedByTable && hasLeftFirstNameNodeOrIsFullRecordRowScopeAccessOrLookup;
        }

        public bool CheckForFullyQualifiedFieldAccess(bool isRHSDelegableTable, BinaryOpNode binaryOpNode, TexlBinding binding, TexlNode node, ref DName columnName, ref FirstNameInfo info)
        {
            Contracts.AssertValue(binaryOpNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(node);

            // Check for fully qualified field access
            var firstNameNode = isRHSDelegableTable ? binaryOpNode.Left?.AsFirstName() : binaryOpNode.Right?.AsFirstName();
            var dottedNameNode = isRHSDelegableTable ? binaryOpNode.Left?.AsDottedName() : binaryOpNode.Right?.AsDottedName();

            if (dottedNameNode != null && dottedNameNode.Left is FirstNameNode possibleScopeAccess && (info = binding.GetInfo(possibleScopeAccess))?.Kind == BindKind.LambdaFullRecord)
            {
                columnName = dottedNameNode.Right.Name;
            }
            else if (firstNameNode == null)
            {
                SuggestDelegationHint(node, binding, TexlStrings.SuggestRemoteExecutionHint_InOpRhs);
                TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage("RHS not valid delegation target"), _binaryOpNode.Right, binding);
                return false;
            }
            else
            {
                info = binding.GetInfo(firstNameNode);
                if (info == null)
                {
                    SuggestDelegationHint(node, binding, TexlStrings.SuggestRemoteExecutionHint_InOpRhs);
                    var structure = StructuralPrint.Print(binding.Top, binding);
                    TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage($"RHS unbound delegation target in rule: {structure}"), _binaryOpNode.Right, binding);
                    return false;
                }

                columnName = info.Name;
            }

            return true;
        }

        public bool IsRowScopedOrLambda(TexlBinding binding, TexlNode node, FirstNameInfo info, DName columnName, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(info);

            // Only rowscoped and lambda scoped nodes are supported on rhs
            // Note: In certain error cases, info may be null.
            var bindKind = info.Kind;
            if (bindKind != BindKind.LambdaField && bindKind != BindKind.LambdaFullRecord)
            {
                SuggestDelegationHint(node, binding, TexlStrings.SuggestRemoteExecutionHint_InOpRhs);
                TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage("RHS not RowScope or LambdaParam"), _binaryOpNode.Right, binding);
                return false;
            }

            IDelegationMetadata columnMetadata = info.Data as DelegationMetadata.DelegationMetadataBase;

            // For this to be delegable, rhs needs to be a column that belongs to innermost scoped delegable datasource.
            if (columnMetadata == null || info.UpCount != 0)
            {
                SuggestDelegationHint(node, binding, TexlStrings.SuggestRemoteExecutionHint_InOpInvalidColumn);
                TrackingProvider.Instance.AddSuggestionMessage(FormatTelemetryMessage("RHS not delegable node"), _binaryOpNode.Right, binding);
                return false;
            }

            var columnPath = DPath.Root.Append(columnName);

            if (!columnMetadata.FilterDelegationMetadata.IsDelegationSupportedByColumn(columnPath, DelegationCapability.Contains) &&
                !columnMetadata.FilterDelegationMetadata.IsDelegationSupportedByColumn(columnPath, DelegationCapability.IndexOf | DelegationCapability.GreaterThan) &&
                !columnMetadata.FilterDelegationMetadata.IsDelegationSupportedByColumn(columnPath, DelegationCapability.SubStringOf | DelegationCapability.Equal))
            {
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, FormatTelemetryMessage("Not supported by column."), TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn, CharacterUtils.MakeSafeForFormatString(columnName.Value));
                return false;
            }

            return true;
        }

        public override bool IsOpSupportedByColumn(OperationCapabilityMetadata metadata, TexlNode column, DPath columnPath, TexlBinding binding)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);

            if (!metadata.IsBinaryOpInDelegationSupported(Op))
            {
                SuggestDelegationHint(column, binding, TexlStrings.OpNotSupportedByClientSuggestionMessage_OpNotSupportedByClient, Op.ToString());
                return false;
            }

            var nodeType = binding.GetType(column);
            var hasEnhancedDelegation = binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled;
            return DType.String.Accepts(nodeType) || nodeType.CoercesTo(DType.String) || (hasEnhancedDelegation && nodeType.IsMultiSelectOptionSet() && nodeType.IsTable);
        }

        public override bool IsOpSupportedByTable(OperationCapabilityMetadata metadata, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            if (!metadata.IsBinaryOpInDelegationSupported(Op))
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByClientSuggestionMessage_OpNotSupportedByClient, Op.ToString());
                return false;
            }

            // RHS always needs to be firstname node or dottedname lambda access to support delegation.
            var isRHSFirstName = _binaryOpNode.Right.Kind == NodeKind.FirstName;
            var isRHSRecordScope = binding.IsFullRecordRowScopeAccess(_binaryOpNode.Right);

            // Check if this is a table delegation for CDS in operator
            var isCdsInTableDelegation = binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled && metadata.IsDelegationSupportedByTable(DelegationCapability.CdsIn) &&
                /* Left node can be first name, row scope lambda or a lookup column */
                (_binaryOpNode.Left.Kind == NodeKind.FirstName || binding.IsFullRecordRowScopeAccess(_binaryOpNode.Left) || (_binaryOpNode.Left.Kind == NodeKind.DottedName && binding.GetType((_binaryOpNode.Left as DottedNameNode).Left).HasExpandInfo)) &&
                /* Right has to be a single column table */
                ((_binaryOpNode.Right.Kind == NodeKind.Table || binding.GetType(_binaryOpNode.Right)?.IsColumn == true) && !binding.IsAsync(_binaryOpNode.Right));

            if (!(isRHSFirstName || isRHSRecordScope || isCdsInTableDelegation))
            {
                return false;
            }

            var supported = metadata.IsDelegationSupportedByTable(DelegationCapability.Contains) || metadata.IsDelegationSupportedByTable(DelegationCapability.CdsIn) ||
                metadata.IsDelegationSupportedByTable(DelegationCapability.IndexOf | DelegationCapability.GreaterThan) ||
                metadata.IsDelegationSupportedByTable(DelegationCapability.SubStringOf | DelegationCapability.Equal);

            if (!supported)
            {
                SuggestDelegationHint(node, binding, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Op.ToString());
                return false;
            }

            return true;
        }
    }
}
