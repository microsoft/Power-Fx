﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies
{
    internal interface ICallNodeDelegatableNodeValidationStrategy
    {
        bool IsValidCallNode(CallNode node, TexlBinding binding, OperationCapabilityMetadata metadata);
    }

    internal interface IDottedNameNodeDelegatableNodeValidationStrategy
    {
        bool IsValidDottedNameNode(DottedNameNode node, TexlBinding binding, OperationCapabilityMetadata metadata, IOpDelegationStrategy opDelStrategy);
    }

    internal interface IFirstNameNodeDelegatableNodeValidationStrategy
    {
        bool IsValidFirstNameNode(FirstNameNode node, TexlBinding binding, IOpDelegationStrategy opDelStrategy);
    }

    internal class DelegationValidationStrategy
        : ICallNodeDelegatableNodeValidationStrategy, IDottedNameNodeDelegatableNodeValidationStrategy, IFirstNameNodeDelegatableNodeValidationStrategy
    {
        private readonly TexlFunction _function;
        public DelegationValidationStrategy(TexlFunction function)
        {
            Contracts.AssertValue(function);

            _function = function;
        }
        protected TexlFunction Function => _function;

        protected void AddSuggestionMessageToTelemetry(string telemetryMessage, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertNonEmpty(telemetryMessage);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            var message = string.Format("Function:{0}, Message:{1}", _function.Name, telemetryMessage);
            TrackingProvider.Instance.AddSuggestionMessage(message, node, binding);
        }

        protected void SuggestDelegationHintAndAddTelemetryMessage(TexlNode node, TexlBinding binding, string telemetryMessage, ErrorResourceKey? suggestionKey = null, params object[] args)
        {
            Contracts.Assert(suggestionKey == null || suggestionKey?.Key != string.Empty);

            SuggestDelegationHint(node, binding, suggestionKey, args);
            AddSuggestionMessageToTelemetry(telemetryMessage, node, binding);
        }

        // Helper used to provide hints when we detect non-delegable parts of the expression due to server restrictions.
        protected void SuggestDelegationHint(TexlNode node, TexlBinding binding, ErrorResourceKey? suggestionKey, params object[] args)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.Assert(suggestionKey == null || suggestionKey?.Key != string.Empty);

            if (suggestionKey == null)
                suggestionKey = TexlStrings.SuggestRemoteExecutionHint;

            if (args == null || args.Length == 0)
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, (ErrorResourceKey)suggestionKey, _function.Name);
            else
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, (ErrorResourceKey)suggestionKey, args);
        }

        protected void SuggestDelegationHint(TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            SuggestDelegationHint(node, binding, null);
        }

        private bool IsValidRowScopedDottedNameNode(DottedNameNode node, TexlBinding binding, OperationCapabilityMetadata metadata, out bool isRowScopedDelegationExempted)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            isRowScopedDelegationExempted = false;
            if (node.Left.Kind == NodeKind.FirstName
                && binding.IsDelegationExempted(node.Left as FirstNameNode)
                && binding.IsLambdaScoped(node.Left as FirstNameNode))
            {
                isRowScopedDelegationExempted = true;

                return true;
            }

            if (node.Left.Kind == NodeKind.DottedName)
                return IsValidRowScopedDottedNameNode(node.Left.AsDottedName(), binding, metadata, out isRowScopedDelegationExempted);

            if (node.Left.Kind == NodeKind.Call && binding.GetInfo(node.Left as CallNode).Function is AsTypeFunction)
                return IsValidCallNode(node.Left as CallNode, binding, metadata);

            // We only allow dotted or firstname node on LHS for now, with exception of AsType.
            return node.Left.Kind == NodeKind.FirstName;
        }

        private OperationCapabilityMetadata GetScopedOperationCapabilityMetadata(IDelegationMetadata delegationMetadata)
        {
            if (Function.FunctionDelegationCapability.HasCapability(DelegationCapability.Sort) ||
                Function.FunctionDelegationCapability.HasCapability(DelegationCapability.SortAscendingOnly))
            {
                return delegationMetadata.SortDelegationMetadata;
            }

            return delegationMetadata.FilterDelegationMetadata;
        }

        public bool IsValidDottedNameNode(DottedNameNode node, TexlBinding binding, OperationCapabilityMetadata metadata, IOpDelegationStrategy opDelStrategy)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValueOrNull(opDelStrategy);

            var isRowScoped = binding.IsRowScope(node);
            if (!isRowScoped)
                return IsValidNode(node, binding);

            bool isRowScopedDelegationExempted;
            if (!IsValidRowScopedDottedNameNode(node, binding, metadata, out isRowScopedDelegationExempted))
            {
                var telemetryMessage = string.Format("Kind:{0}, isRowScoped:{1}",
                    node.Kind, isRowScoped);

                SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
                return false;
            }

            if (isRowScopedDelegationExempted)
            {
                binding.SetBlockScopedConstantNode(node);
                return true;
            }

            if (binding.TryGetFullRecordRowScopeAccessInfo(node, out var firstNameInfo))
            {
                // This means that this row scoped field is from some parent scope which is non-delegatable. That should deny delegation at that point.
                // For this scope, this means that value will be provided from some other source.
                // For example, AddColumns(CDS As Left, "Column1", LookUp(CDS1, Left.Name in FirstName))
                // CDS - *[Name:s], CDS1 - *[FirstName:s]
                if (GetCapabilityMetadata(firstNameInfo) == null)
                    return true;
            }

            if (!binding.GetType(node.Left).HasExpandInfo)
            {
                DPath columnPath;
                if (!BinderUtils.TryConvertNodeToDPath(binding, node, out columnPath) || !metadata.IsDelegationSupportedByColumn(columnPath, _function.FunctionDelegationCapability))
                {
                    var safeColumnName = CharacterUtils.MakeSafeForFormatString(columnPath.ToDottedSyntax());
                    var message = string.Format(StringResources.Get(TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn), safeColumnName);
                    SuggestDelegationHintAndAddTelemetryMessage(node, binding, message, TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn, safeColumnName);
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.NoDelSupportByColumn, node, binding, _function, DelegationTelemetryInfo.CreateNoDelSupportByColumnTelemetryInfo(columnPath.ToDottedSyntax()));
                    return false;
                }

                // If there is any operator applied on this node then check if column supports operation.
                return opDelStrategy?.IsOpSupportedByColumn(metadata, node, columnPath, binding) ?? true;
            }

            // If there is an entity reference then we need to do additional verification.
            IExpandInfo info = binding.GetType(node.Left).ExpandInfo.VerifyValue();
            var dataSourceInfo = info.ParentDataSource;

            IDataEntityMetadata entityMetadata;
            if (!dataSourceInfo.DataEntityMetadataProvider.TryGetEntityMetadata(info.Identity, out entityMetadata))
            {
                var telemetryMessage = string.Format("Kind:{0}, isRowScoped:{1}, no metadata found for entity {2}",
                    node.Kind, isRowScoped, CharacterUtils.MakeSafeForFormatString(info.Identity));

                SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
                return false;
            }

            OperationCapabilityMetadata entityCapabilityMetadata = GetScopedOperationCapabilityMetadata(entityMetadata.DelegationMetadata);
            string maybeLogicalName;
            DName columnName = node.Right.Name;
            if (entityMetadata.DisplayNameMapping.TryGetFromSecond(node.Right.Name.Value, out maybeLogicalName))
                columnName = new DName(maybeLogicalName);

            var entityColumnPath = DPath.Root.Append(columnName);

            if (!entityCapabilityMetadata.IsDelegationSupportedByColumn(entityColumnPath, _function.FunctionDelegationCapability))
            {
                var safeColumnName = CharacterUtils.MakeSafeForFormatString(columnName.Value);
                var message = string.Format(StringResources.Get(TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn), safeColumnName);
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, message, TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn, safeColumnName);
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.NoDelSupportByColumn, node, binding, _function, DelegationTelemetryInfo.CreateNoDelSupportByColumnTelemetryInfo(columnName));
                return false;
            }

            // If there is any operator applied on this node then check if column supports operation.
            return opDelStrategy?.IsOpSupportedByColumn(entityCapabilityMetadata, node, entityColumnPath, binding) ?? true;
        }

        public bool IsValidFirstNameNode(FirstNameNode node, TexlBinding binding, IOpDelegationStrategy opDelStrategy)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValueOrNull(opDelStrategy);

            var isRowScoped = binding.IsRowScope(node);
            var isValid = IsValidNode(node, binding);
            if (isValid && !isRowScoped)
                return true;

            // If invalid node then return immediately.
            if (!isValid)
                return false;

            return IsDelegatableColumnNode(node, binding, opDelStrategy, _function.FunctionDelegationCapability);
        }

        private IDelegationMetadata GetCapabilityMetadata(FirstNameInfo info)
        {
            Contracts.AssertValue(info);

            IDelegationMetadata metadata = null;
            if (info.Data is DelegationMetadata.DelegationMetadata)
                return (info.Data as DelegationMetadata.DelegationMetadata);

            if (info.Data is IExpandInfo)
            {
                var entityInfo = (info.Data as IExpandInfo).VerifyValue();
                Contracts.AssertValue(entityInfo.ParentDataSource);
                Contracts.AssertValue(entityInfo.ParentDataSource.DataEntityMetadataProvider);

                var metadataProvider = entityInfo.ParentDataSource.DataEntityMetadataProvider;

                IDataEntityMetadata entityMetadata;
                var result = metadataProvider.TryGetEntityMetadata(entityInfo.Identity, out entityMetadata);
                Contracts.Assert(result);

                metadata = entityMetadata.VerifyValue().DelegationMetadata.VerifyValue();
            }

            return metadata;
        }

        // Verifies if provided column node supports delegation.
        protected bool IsDelegatableColumnNode(FirstNameNode node, TexlBinding binding, IOpDelegationStrategy opDelStrategy, DelegationCapability capability)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValueOrNull(opDelStrategy);
            Contracts.Assert(binding.IsRowScope(node));

            FirstNameInfo firstNameInfo = binding.GetInfo(node.AsFirstName());
            if (firstNameInfo == null)
                return false;

            IDelegationMetadata metadata = GetCapabilityMetadata(firstNameInfo);
            // This means that this row scoped field is from some parent scope which is non-delegatable. That should deny delegation at that point.
            // For this scope, this means that value will be provided from some other source.
            // For example, AddColumns(CDS, "Column1", LookUp(CDS1, Name in FirstName))
            // CDS - *[Name:s], CDS1 - *[FirstName:s]
            if (metadata == null)
                return true;

            var columnName = firstNameInfo.Name;
            Contracts.AssertValid(columnName);

            var columnPath = DPath.Root.Append(columnName);

            if (!metadata.FilterDelegationMetadata.IsDelegationSupportedByColumn(columnPath, capability))
            {
                var safeColumnName = CharacterUtils.MakeSafeForFormatString(columnName.Value);
                var message = string.Format(StringResources.Get(TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn), safeColumnName);
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, message, TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn, safeColumnName);
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.NoDelSupportByColumn, node, binding, _function, DelegationTelemetryInfo.CreateNoDelSupportByColumnTelemetryInfo(firstNameInfo));
                return false;
            }

            // If there is any operator applied on this node then check if column supports operation.
            if (opDelStrategy != null && !opDelStrategy.IsOpSupportedByColumn(metadata.FilterDelegationMetadata, node.AsFirstName(), columnPath, binding))
                return false;

            return true;
        }

        public bool IsValidCallNode(CallNode node, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            if (!IsValidNode(node, binding))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            // If the node is not row scoped and it's valid then it can be delegated.
            var isRowScoped = binding.IsRowScope(node);
            if (!isRowScoped)
                return true;

            CallInfo callInfo = binding.GetInfo(node);
            if (callInfo?.Function != null && ((TexlFunction)callInfo.Function).IsRowScopedServerDelegatable(node, binding, metadata))
                return true;

            var telemetryMessage = string.Format("Kind:{0}, isRowScoped:{1}", node.Kind, isRowScoped);
            SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);
            TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.UndelegatableFunction, node, binding, _function, DelegationTelemetryInfo.CreateUndelegatableFunctionTelemetryInfo((TexlFunction)callInfo?.Function));
            return false;
        }

        protected bool IsValidNode(TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            bool isAsync = binding.IsAsync(node);
            bool isPure = binding.IsPure(node);


            if (node is DottedNameNode &&
                ((binding.GetType(node.AsDottedName().Left).Kind == DKind.OptionSet && binding.GetType(node).Kind == DKind.OptionSetValue) ||
                (binding.GetType(node.AsDottedName().Left).Kind == DKind.View && binding.GetType(node).Kind == DKind.ViewValue)))
            {
                // OptionSet and View Access are delegable despite being async
                return true;
            }

            if (node is CallNode && (binding.IsBlockScopedConstant(node) ||
                (binding.GetInfo(node as CallNode).Function is AsTypeFunction)))
                // AsType is delegable despite being async
                return true;

            // Async predicates and impure nodes are not supported.
            // Let CallNodes for User() be marked as being Valid to allow
            // expressions with User() calls to be delegated
            if (!(IsUserCallNodeDelegable(node, binding)) && (isAsync || !isPure))
            {
                var telemetryMessage = string.Format("Kind:{0}, isAsync:{1}, isPure:{2}", node.Kind, isAsync, isPure);
                SuggestDelegationHintAndAddTelemetryMessage(node, binding, telemetryMessage);

                if (isAsync)
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.AsyncPredicate, node, binding, _function);

                if (!isPure)
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.ImpureNode, node, binding, _function, DelegationTelemetryInfo.CreateImpureNodeTelemetryInfo(node, binding));

                return false;
            }

            return true;
        }

        private bool IsUserCallNodeDelegable(TexlNode node, TexlBinding binding)
        {
            if ((node is DottedNameNode)
                && (node.AsDottedName().Left is CallNode)
                && (binding.GetInfo(node.AsDottedName().Left.AsCall()).Function is ICustomDelegationFunction customDelFunc)
                && customDelFunc.IsUserCallNodeDelegable())
            {
                return true;
            }

            return false;
        }
    }
}
