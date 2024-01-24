// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    internal sealed class YamlTexlFunction : YamlReaderWriter, IYamlFunction
    {
        public YamlTexlFunction()
        {
        }

        internal YamlTexlFunction(TexlFunction texlFunction, bool isLibrary)
        {
            Name = texlFunction.Name;
            LocaleInvariantName = texlFunction.LocaleInvariantName;
            LocaleSpecificName = texlFunction.LocaleSpecificName;
            Description = texlFunction.Description;
            MinArity = texlFunction.MinArity;
            MaxArity = texlFunction.MaxArity;

            string[] paramTypes = texlFunction.ParamTypes.Select(pt => pt.ToString()).ToArray();

            List<YamlTexlSignature> signatures = new List<YamlTexlSignature>();

            foreach (TexlStrings.StringGetter[] signature in texlFunction.GetSignatures())
            {
                List<YamlTexlParam> requiredParams = new List<YamlTexlParam>();
                List<YamlTexlParam> optionalParams = new List<YamlTexlParam>();

                string[] paramNames = signature.Select(sg => sg(null)).ToArray();

                for (int i = 0; i < paramNames.Length; i++)
                {
                    (i < texlFunction.MinArity ? requiredParams : optionalParams).Add(new YamlTexlParam()
                    {
                        Name = paramNames[i],
                        Type = i < paramTypes.Length
                                    ? paramTypes[i]
                                    : isLibrary && texlFunction.IsVariadicFunction
                                    ? "Variadic"
                                    : isLibrary && texlFunction.FunctionCategoriesMask.HasFlag(FunctionCategories.Table)
                                    ? "FromTable"
                                    : isLibrary && (texlFunction.Name == "Error" || texlFunction.Name == "IsError")
                                    ? "Error"
                                    : isLibrary && texlFunction.Name == "IsNumeric"
                                    ? "Any"
                                    : throw new Exception($"Unexpected function {texlFunction.Name} / {texlFunction.GetType().Name}")
                    });
                }

                signatures.Add(new YamlTexlSignature()
                {
                    RequiredParameters = requiredParams.ToArray(),
                    OptionalParameters = optionalParams.ToArray()
                });
            }

            SignatureCount = signatures.Count;
            Signatures = signatures.ToArray();
            ReturnType = texlFunction.ReturnType.ToString(); //  DType
            RequiredEnumNames = string.Join(", ", texlFunction.GetRequiredEnumNames());
            Capabilities = ToCapabilitiesString(texlFunction.Capabilities);
            FunctionCategoriesMask = ToFunctionCategoriesString(texlFunction.FunctionCategoriesMask);
            FunctionDelegationCapability = ToDelegationCapabilityString(texlFunction.FunctionDelegationCapability);
            FunctionPermission = ToFunctionPermissionString(texlFunction.FunctionPermission);
            ScopeInfo = texlFunction.ScopeInfo == null ? null : new YamlTexlScopeInfo(texlFunction.ScopeInfo);

            AffectsAliases = texlFunction.AffectsAliases;
            AffectsCollectionSchemas = texlFunction.AffectsCollectionSchemas;
            AffectsDataSourceQueryOptions = texlFunction.AffectsDataSourceQueryOptions;
            AffectsScopeVariable = texlFunction.AffectsScopeVariable;
            AllowedWithinNondeterministicOperationOrder = texlFunction.AllowedWithinNondeterministicOperationOrder;
            CanReturnExpandInfo = texlFunction.CanReturnExpandInfo;
            CanSuggestContextVariables = texlFunction.CanSuggestContextVariables;
            CanSuggestInputColumns = texlFunction.CanSuggestInputColumns;
            HasColumnIdentifiers = texlFunction.HasColumnIdentifiers;
            HasEcsExcemptLambdas = texlFunction.HasEcsExcemptLambdas;
            HasLambdas = texlFunction.HasLambdas;
            HasPreciseErrors = texlFunction.HasPreciseErrors;
            HelpLink = texlFunction.HelpLink;
            IsAsync = texlFunction.IsAsync;
            IsAutoRefreshable = texlFunction.IsAutoRefreshable;
            IsBehaviorOnly = texlFunction.IsBehaviorOnly;
            IsDeprecatedOrInternalFunction = texlFunction.IsDeprecatedOrInternalFunction;
            IsDynamic = texlFunction.IsDynamic;
            IsGlobalReliant = texlFunction.IsGlobalReliant;
            IsHidden = texlFunction.IsHidden;
            IsPure = texlFunction.IsPure;
            IsSelfContained = texlFunction.IsSelfContained;
            IsStateless = texlFunction.IsStateless;
            IsStrict = texlFunction.IsStrict;
            IsTestOnly = texlFunction.IsTestOnly;
            IsVariadicFunction = texlFunction.IsVariadicFunction;
            ManipulatesCollections = texlFunction.ManipulatesCollections;
            ModifiesValues = texlFunction.ModifiesValues;
            MutatesArg0 = texlFunction.MutatesArg0;
            PropagatesMutability = texlFunction.PropagatesMutability;
            RequireAllParamColumns = texlFunction.RequireAllParamColumns;
            RequiresDataSourceScope = texlFunction.RequiresDataSourceScope;
            ShowAIDisclaimer = texlFunction.ShowAIDisclaimer;
            SignatureConstraint = texlFunction.SignatureConstraint == null ? null : new YamlTexlSignatureConstraint()
            {
                EndNonRepeatCount = texlFunction.SignatureConstraint.EndNonRepeatCount,
                OmitStartIndex = texlFunction.SignatureConstraint.OmitStartIndex,
                RepeatSpan = texlFunction.SignatureConstraint.RepeatSpan,
                RepeatTopLength = texlFunction.SignatureConstraint.RepeatTopLength
            };
            SkipScopeForInlineRecords = texlFunction.SkipScopeForInlineRecords;
            SuggestionTypeReferenceParamIndex = texlFunction.SuggestionTypeReferenceParamIndex;
            SupportsMetadataTypeArg = texlFunction.SupportsMetadataTypeArg;
            SupportsParamCoercion = texlFunction.SupportsParamCoercion;
            UseParentScopeForArgumentSuggestions = texlFunction.UseParentScopeForArgumentSuggestions;
            UsesEnumNamespace = texlFunction.UsesEnumNamespace;
        }

        public string Name;
        public string LocaleInvariantName;
        public string LocaleSpecificName;
        public string Description;
        public int MinArity;
        public int MaxArity;
        public int SignatureCount;
        public YamlTexlSignature[] Signatures;
        public string ReturnType;
        public string RequiredEnumNames;
        public string Capabilities;
        public string FunctionCategoriesMask;
        public string FunctionDelegationCapability;
        public string FunctionPermission;
        public YamlTexlScopeInfo ScopeInfo;
        public bool AffectsAliases;
        public bool AffectsCollectionSchemas;
        public bool AffectsDataSourceQueryOptions;
        public bool AffectsScopeVariable;
        public bool AllowedWithinNondeterministicOperationOrder;
        public bool CanReturnExpandInfo;
        public bool CanSuggestContextVariables;
        public bool CanSuggestInputColumns;
        public bool HasColumnIdentifiers;
        public bool HasEcsExcemptLambdas;
        public bool HasLambdas;
        public bool HasPreciseErrors;
        public string HelpLink;
        public bool IsAsync;
        public bool IsAutoRefreshable;
        public bool IsBehaviorOnly;
        public bool IsDeprecatedOrInternalFunction;
        public bool IsDynamic;
        public bool IsGlobalReliant;
        public bool IsHidden;
        public bool IsPure;
        public bool IsSelfContained;
        public bool IsStateless;
        public bool IsStrict;
        public bool IsTestOnly;
        public bool IsVariadicFunction;
        public bool ManipulatesCollections;
        public bool ModifiesValues;
        public bool MutatesArg0;
        public bool PropagatesMutability;
        public bool RequireAllParamColumns;
        public bool RequiresDataSourceScope;
        public bool ShowAIDisclaimer;
        public YamlTexlSignatureConstraint SignatureConstraint;
        public bool SkipScopeForInlineRecords;
        public int SuggestionTypeReferenceParamIndex;
        public bool SupportsMetadataTypeArg;
        public bool SupportsParamCoercion;
        public bool UseParentScopeForArgumentSuggestions;
        public bool UsesEnumNamespace;

        // Returns list of flags
        private static string ToFunctionPermissionString(RequiredDataSourcePermissions requiredDataSourcePermissions)
        {
            if (requiredDataSourcePermissions == 0)
            {
                return nameof(RequiredDataSourcePermissions.None);
            }

            List<string> flagList = new List<string>();

            if (requiredDataSourcePermissions.HasFlag(RequiredDataSourcePermissions.Create))
            {
                flagList.Add(nameof(RequiredDataSourcePermissions.Create));
            }

            if (requiredDataSourcePermissions.HasFlag(RequiredDataSourcePermissions.Read))
            {
                flagList.Add(nameof(RequiredDataSourcePermissions.Read));
            }

            if (requiredDataSourcePermissions.HasFlag(RequiredDataSourcePermissions.Update))
            {
                flagList.Add(nameof(RequiredDataSourcePermissions.Update));
            }

            if (requiredDataSourcePermissions.HasFlag(RequiredDataSourcePermissions.Delete))
            {
                flagList.Add(nameof(RequiredDataSourcePermissions.Delete));
            }

            return string.Join(", ", flagList.OrderBy(x => x));
        }

        // Returns hex value
        private static string ToDelegationCapabilityString(DelegationCapability delegationCapability)
        {
            return $"0x{delegationCapability.Capabilities.ToString("X", CultureInfo.InvariantCulture)}";
        }

        // Returns list of flags in alpha order
        private static string ToFunctionCategoriesString(FunctionCategories functionCategories)
        {
            if (functionCategories == 0u)
            {
                return nameof(FunctionCategories.None);
            }

            List<string> flagList = new List<string>();

            if (functionCategories.HasFlag(FunctionCategories.Text))
            {
                flagList.Add(nameof(FunctionCategories.Text));
            }

            if (functionCategories.HasFlag(FunctionCategories.Logical))
            {
                flagList.Add(nameof(FunctionCategories.Logical));
            }

            if (functionCategories.HasFlag(FunctionCategories.Table))
            {
                flagList.Add(nameof(FunctionCategories.Table));
            }

            if (functionCategories.HasFlag(FunctionCategories.Behavior))
            {
                flagList.Add(nameof(FunctionCategories.Behavior));
            }

            if (functionCategories.HasFlag(FunctionCategories.DateTime))
            {
                flagList.Add(nameof(FunctionCategories.DateTime));
            }

            if (functionCategories.HasFlag(FunctionCategories.MathAndStat))
            {
                flagList.Add(nameof(FunctionCategories.MathAndStat));
            }

            if (functionCategories.HasFlag(FunctionCategories.Information))
            {
                flagList.Add(nameof(FunctionCategories.Information));
            }

            if (functionCategories.HasFlag(FunctionCategories.Color))
            {
                flagList.Add(nameof(FunctionCategories.Color));
            }

            if (functionCategories.HasFlag(FunctionCategories.REST))
            {
                flagList.Add(nameof(FunctionCategories.REST));
            }

            if (functionCategories.HasFlag(FunctionCategories.Component))
            {
                flagList.Add(nameof(FunctionCategories.Component));
            }

            if (functionCategories.HasFlag(FunctionCategories.UserDefined))
            {
                flagList.Add(nameof(FunctionCategories.UserDefined));
            }

            return string.Join(", ", flagList.OrderBy(x => x));
        }

        // Returns list of flags in alpha order
        private static string ToCapabilitiesString(Capabilities capabilities)
        {
            if (capabilities == 0u)
            {
                return nameof(Core.Functions.Publish.Capabilities.None);
            }

            List<string> flagList = new List<string>();

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.OutboundInternetAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.OutboundInternetAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.PrivateNetworkAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.PrivateNetworkAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.EnterpriseAuthentication))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.EnterpriseAuthentication));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.LocalStateAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.LocalStateAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.PicturesLibraryAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.PicturesLibraryAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.VideoLibraryAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.VideoLibraryAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.MusicLibraryAccess))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.MusicLibraryAccess));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.Camera))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.Camera));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.Microphone))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.Microphone));
            }

            if (capabilities.HasFlag(Core.Functions.Publish.Capabilities.Location))
            {
                flagList.Add(nameof(Core.Functions.Publish.Capabilities.Location));
            }

            return string.Join(", ", flagList.OrderBy(x => x));
        }

        string IYamlFunction.GetName()
        {
            return Name;
        }

        bool IYamlFunction.HasDetailedProperties()
        {
            return false;
        }

        bool IYamlFunction.GetIsSupported()
        {
            throw new NotImplementedException();
        }

        bool IYamlFunction.GetIsDeprecated()
        {
            throw new NotImplementedException();
        }

        bool IYamlFunction.GetIsInternal()
        {
            throw new NotImplementedException();
        }

        bool IYamlFunction.GetIsPageable()
        {
            throw new NotImplementedException();
        }

        string IYamlFunction.GetNotSupportedReason()
        {
            throw new NotImplementedException();
        }

        int IYamlFunction.GetArityMin()
        {
            return MinArity;
        }

        int IYamlFunction.GetArityMax()
        {
            return MaxArity;
        }

        string IYamlFunction.GetRequiredParameterTypes()
        {
            return Signatures?.OrderByDescending(s => (s.RequiredParameters?.Length ?? 0) + (s.OptionalParameters?.Length ?? 0)).First().GetRequiredParameterTypes();
        }

        string IYamlFunction.GetOptionalParameterTypes()
        {
            return Signatures?.OrderByDescending(s => (s.RequiredParameters?.Length ?? 0) + (s.OptionalParameters?.Length ?? 0)).First().GetOptionalParameterTypes();
        }

        string IYamlFunction.GetReturnType()
        {
            return ReturnType;
        }

        string IYamlFunction.GetParameterNames()
        {
            return Signatures?.OrderByDescending(s => (s.RequiredParameters?.Length ?? 0) + (s.OptionalParameters?.Length ?? 0)).First().GetParameterNames();
        }
    }

    internal class YamlTexlSignature
    {
        public YamlTexlParam[] RequiredParameters;
        public YamlTexlParam[] OptionalParameters;

        public string GetParameterNames()
        {
            string paramNames = string.Join(", ", RequiredParameters?.Select(rp => rp.Name) ?? Enumerable.Empty<string>());

            if (OptionalParameters?.Any() == true)
            {
                if (!string.IsNullOrEmpty(paramNames))
                {
                    paramNames += ", ";
                }

                paramNames += $"{string.Join(", ", OptionalParameters.Select(op => op.Name))}";
            }

            return paramNames;
        }

        public string GetRequiredParameterTypes()
        {
            if (RequiredParameters == null || RequiredParameters.Length == 0)
            {
                return null;
            }

            return string.Join(", ", RequiredParameters.Select(rp => rp.Type));
        }

        public string GetOptionalParameterTypes()
        {
            if (OptionalParameters == null || OptionalParameters.Length == 0)
            {
                return null;
            }

            return string.Join(", ", OptionalParameters.Select(op => op.Type));
        }
    }

    internal class YamlTexlParam
    {
        public string Name;
        public string Type;
    }

    internal class YamlTexlScopeInfo
    {
        public YamlTexlScopeInfo()
        {
        }

        internal YamlTexlScopeInfo(FunctionScopeInfo scopeInfo)
        {
            AcceptsLiteralPredicates = scopeInfo.AcceptsLiteralPredicates;
            CanBeCreatedByRecord = scopeInfo.CanBeCreatedByRecord;
            HasNondeterministicOperationOrder = scopeInfo.HasNondeterministicOperationOrder;
            IteratesOverScope = scopeInfo.IteratesOverScope;
            ScopeType = scopeInfo.ScopeType?.ToString() ?? "<null>"; // DType
            SupportsAsyncLambdas = scopeInfo.SupportsAsyncLambdas;
            UsesAllFieldsInScope = scopeInfo.UsesAllFieldsInScope;
        }

        public bool AcceptsLiteralPredicates;
        public bool CanBeCreatedByRecord;
        public bool HasNondeterministicOperationOrder;
        public bool IteratesOverScope;
        public string ScopeType;
        public bool SupportsAsyncLambdas;
        public bool UsesAllFieldsInScope;
    }

    internal class YamlTexlSignatureConstraint
    {
        public int EndNonRepeatCount;
        public int OmitStartIndex;
        public int RepeatSpan;
        public int RepeatTopLength;
    }
}
