using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionComparer : IEqualityComparer<TexlFunction>
    {
        public bool Equals(TexlFunction x, TexlFunction y)
        {
            return x.AffectsAliases == y.AffectsAliases &&
                    x.AffectsCollectionSchemas == y.AffectsCollectionSchemas &&
                    x.AffectsDataSourceQueryOptions == y.AffectsDataSourceQueryOptions &&
                    x.AffectsScopeVariable == y.AffectsScopeVariable &&
                    x.AllowedWithinNondeterministicOperationOrder == y.AllowedWithinNondeterministicOperationOrder &&
                    x.CanBeUsedInTests == y.CanBeUsedInTests &&
                    x.CanReturnExpandInfo == y.CanReturnExpandInfo &&
                    x.CanSuggestContextVariables == y.CanSuggestContextVariables &&
                    x.CanSuggestInputColumns == y.CanSuggestInputColumns &&
                    x.CanSuggestThisItem == y.CanSuggestThisItem &&
                    x.Capabilities == y.Capabilities &&
                    x.CreatesImplicitScreenDependency == y.CreatesImplicitScreenDependency &&
                    x.Description == y.Description &&
                    x.DisableForCommanding == y.DisableForCommanding &&
                    x.DisableForComponent == y.DisableForComponent &&
                    x.DisableForDataComponent == y.DisableForDataComponent &&
                    x.FunctionCategoriesMask == y.FunctionCategoriesMask &&
                    x.FunctionDelegationCapability.Capabilities == y.FunctionDelegationCapability.Capabilities &&
                    x.FunctionPermission == y.FunctionPermission &&
                    x.HasEcsExcemptLambdas == y.HasEcsExcemptLambdas &&
                    x.HasLambdas == y.HasLambdas &&
                    x.HasPreciseErrors == y.HasPreciseErrors &&
                    x.IsAsync == y.IsAsync &&
                    x.IsAutoRefreshable == y.IsAutoRefreshable &&
                    x.IsBehaviorOnly == y.IsBehaviorOnly &&
                    x.IsDynamic == y.IsDynamic &&
                    x.IsGlobalReliant == y.IsGlobalReliant &&
                    x.IsHidden == y.IsHidden &&
                    x.IsSelfContained == y.IsSelfContained &&
                    x.IsStateless == y.IsStateless &&
                    x.IsStrict == y.IsStrict &&
                    x.IsTestOnly == y.IsTestOnly &&
                    x.IsTrackedInTelemetry == x.IsTrackedInTelemetry &&
                    x.LocaleInvariantName == y.LocaleInvariantName &&
                    x.LocaleSpecificName == y.LocaleSpecificName &&
                    x.LocaleSpecificNamespace == y.LocaleSpecificNamespace &&
                    x.ManipulatesCollections == y.ManipulatesCollections &&
                    x.MaxArity == y.MaxArity &&
                    x.MinArity == y.MinArity &&
                    x.ModifiesValues == y.ModifiesValues &&
                    x.ParamTypes.SequenceEqual(y.ParamTypes) &&
                    x.QualifiedName == y.QualifiedName &&
                    x.RequireAllParamColumns == y.RequireAllParamColumns &&
                    x.RequiresBindingContext == y.RequiresBindingContext &&
                    x.RequiresDataSourceScope == y.RequiresDataSourceScope &&
                    x.ReturnType == y.ReturnType &&
                    x.ScopeInfo == y.ScopeInfo &&
                    x.SkipScopeForInlineRecords == y.SkipScopeForInlineRecords &&
                    x.SuggestionTypeReferenceParamIndex == y.SuggestionTypeReferenceParamIndex &&
                    x.SupportsInlining == y.SupportsInlining &&
                    x.SupportsMetadataTypeArg == y.SupportsMetadataTypeArg &&
                    x.SupportsParamCoercion == y.SupportsParamCoercion &&
                    x.SuppressIntellisenseForComponent == x.SuppressIntellisenseForComponent &&
                    x.UseParentScopeForArgumentSuggestions == y.UseParentScopeForArgumentSuggestions &&
                    x.UsesEnumNamespace == y.UsesEnumNamespace;
        }

        public int GetHashCode(TexlFunction obj)
        {
            return obj.GetHashCode();
        }
    }
}
