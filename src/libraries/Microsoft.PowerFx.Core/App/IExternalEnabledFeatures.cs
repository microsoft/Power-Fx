namespace Microsoft.PowerFx.Core.App
{
    /// <summary>
    /// All hosts should use <see cref="DefaultEnabledFeatures"/> except for Canvas Apps (legacy flag support)
    /// This interface and the flags in it should be deprecated once the below flags are removed from Canvas Apps
    /// DO NOT add flags to this clas without very strong justification. We do not want to allow PowerFx
    /// behavior to be different between target platforms. 
    /// </summary>
    public interface IExternalEnabledFeatures
    {
        bool IsEnhancedDelegationEnabled { get; }
        bool IsProjectionMappingEnabled { get; }
        bool IsEnableRowScopeOneToNExpandEnabled { get; }
        bool IsUseDisplayNameMetadataEnabled { get; }
        bool IsDynamicSchemaEnabled { get; }
    }

    public class DefaultEnabledFeatures : IExternalEnabledFeatures
    {
        public bool IsEnhancedDelegationEnabled => true;
        public bool IsProjectionMappingEnabled => true;
        public bool IsEnableRowScopeOneToNExpandEnabled => true;
        public bool IsUseDisplayNameMetadataEnabled => true;
        public bool IsDynamicSchemaEnabled => true;
    }
}
