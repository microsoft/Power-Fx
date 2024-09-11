// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.App
{
    internal interface IECSFlags
    {
        internal bool IsNewEnhancedComponentPropertiesFlightEnabled { get; }

        internal bool IsComponentFunctionPropertyDataflowEnabled { get; }

        internal bool IsCommentingImportExportEnabled { get; }

        internal bool ShowPowerFxV1FeatureECSFlag { get; }

        internal bool IsUpdateIfDelegationEnabled { get; }

        internal bool EnableSecureSharedConnectionsByDefault { get; }

        internal bool IsComponentResetBehaviorUpdated { get; }

        internal bool IsPostCompLibImportAnalysisEnabled { get; }

        internal bool IsLazyRecordLoadingEnabled { get; }

        internal bool IsAsyncNodeDelegationEnabled { get; }

        internal bool IsImpureNodeDelegationEnabled { get; }

        internal bool IsDataflowAnalysisVisible { get; }

        internal bool IsDataflowAnalysisEnabledForNewApps { get; }

        internal bool IsDataflowAnalysisEnabledForAllApps { get; }

        internal bool IsAsyncOnComponentFunctionProperties { get; }

        internal bool IsCanvasComponentBehaviorPropertyCoercion { get; }

        internal bool IsModernControlsOnByDefault { get; }

        internal bool IsPowerFxFormulaBarCommentsToFxEnabledV2 { get; }

        internal bool IsPowerFxFormulaBarCommentsToFxEnabledForNewApps { get; }

        internal bool IsRemoveAllDelegationEnabled { get; }

        internal bool IsEditInMCSECSEnabled { get; }

        internal bool IsImperativeUdfEnabled { get; }

        internal bool IsNL2FxReducedContextEnabled { get; }

        internal bool IsProactiveControlRenameEnabled { get; }
    }
}
