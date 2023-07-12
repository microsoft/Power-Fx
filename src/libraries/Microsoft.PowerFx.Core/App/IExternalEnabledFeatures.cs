﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.App
{
    /// <summary>
    /// All hosts should use <see cref="DefaultEnabledFeatures"/> except for Canvas Apps (legacy flag support)
    /// This interface and the flags in it should be deprecated once the below flags are removed from Canvas Apps
    /// DO NOT add flags to this clas without very strong justification. We do not want to allow PowerFx
    /// behavior to be different between target platforms. 
    /// </summary>
    internal interface IExternalEnabledFeatures
    {
        bool IsEnhancedDelegationEnabled { get; }

        bool IsProjectionMappingEnabled { get; }

        bool IsEnableRowScopeOneToNExpandEnabled { get; }

        bool IsUseDisplayNameMetadataEnabled { get; }

        bool IsDynamicSchemaEnabled { get; }

        bool IsEnhancedComponentFunctionPropertyEnabled { get; }

        bool IsComponentFunctionPropertyDataflowEnabled { get; }
    }

    internal sealed class DefaultEnabledFeatures : IExternalEnabledFeatures
    {
        public bool IsEnhancedDelegationEnabled => true;

        public bool IsProjectionMappingEnabled => true;

        public bool IsEnableRowScopeOneToNExpandEnabled => true;

        public bool IsUseDisplayNameMetadataEnabled => true;

        public bool IsDynamicSchemaEnabled => true;

        public bool IsEnhancedComponentFunctionPropertyEnabled => true;

        public bool IsComponentFunctionPropertyDataflowEnabled => true;
    }
}
