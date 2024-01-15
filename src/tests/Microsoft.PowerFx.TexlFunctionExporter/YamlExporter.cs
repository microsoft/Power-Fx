// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using YamlDotNet.Serialization;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public static class YamlExporter
    {
        // internal function as TexlFunction is internal
        internal static void ExportTexlFunction(string folder, TexlFunction texlFunction, bool isLibrary = false)
        {
            ExpandoObject obj = texlFunction.ToExpando(isLibrary);
            ISerializer serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(obj);
            string funcName = isLibrary ? texlFunction.GetType().Name : texlFunction.Name;

            if (isLibrary)
            {
                if (funcName.EndsWith("Function", StringComparison.Ordinal))
                {
                    funcName = funcName.Substring(0, funcName.Length - 8);
                }

                if (funcName.EndsWith("Function_UO", StringComparison.Ordinal))
                {
                    funcName = $"{funcName.Substring(0, funcName.Length - 11)}_UO";
                }

                if (funcName.EndsWith("Function_T", StringComparison.Ordinal))
                {
                    funcName = $"{funcName.Substring(0, funcName.Length - 10)}_T";
                }

                if (funcName != texlFunction.Name)
                {
                    funcName = $"{texlFunction.Name}_{funcName}";
                }
            }

            string functionFile = Path.Combine(folder, funcName.Replace("/", "_", StringComparison.OrdinalIgnoreCase) + ".yaml");
            Directory.CreateDirectory(folder);

            if (File.Exists(functionFile))
            {
                throw new IOException($"File {functionFile} already exists!");
            }

            File.WriteAllText(functionFile, yaml, Encoding.UTF8);
        }

        private static ExpandoObject ToExpando(this TexlFunction texlFunction, bool isLibrary)
        {
            dynamic texlFunction_ex = new ExpandoObject();

            texlFunction_ex.Name = texlFunction.Name;
            texlFunction_ex.LocaleInvariantName = texlFunction.LocaleInvariantName;
            texlFunction_ex.LocaleSpecificName = texlFunction.LocaleSpecificName;
            texlFunction_ex.Description = texlFunction.Description;
            texlFunction_ex.MinArity = texlFunction.MinArity;
            texlFunction_ex.MaxArity = texlFunction.MaxArity;

            string[] paramTypes = texlFunction.ParamTypes.Select(pt => pt.ToString()).ToArray();
            TexlStrings.StringGetter[][] signatures = texlFunction.GetSignatures().ToArray();
            List<dynamic> signatures_ex = new List<dynamic>();

            foreach (TexlStrings.StringGetter[] signature in signatures)
            {
                List<dynamic> requiredParams_ex = new List<dynamic>();
                List<dynamic> optionalParams_ex = new List<dynamic>();

                string[] paramNames = signature.Select(sg => sg(null)).ToArray();

                for (int i = 0; i < paramNames.Length; i++)
                {
                    dynamic parameter_ex = new ExpandoObject();
                    parameter_ex.Name = paramNames[i];
                    parameter_ex.Type = i < paramTypes.Length
                                        ? paramTypes[i]
                                        : isLibrary && texlFunction.IsVariadicFunction
                                        ? "Variadic"
                                        : isLibrary && texlFunction.FunctionCategoriesMask.HasFlag(FunctionCategories.Table)
                                        ? "FromTable"
                                        : isLibrary && (texlFunction.Name == "Error" || texlFunction.Name == "IsError")
                                        ? "Error"
                                        : isLibrary && texlFunction.Name == "IsNumeric"
                                        ? "Any"
                                        : throw new Exception($"Unexpected function {texlFunction.Name} / {texlFunction.GetType().Name}");

                    (i < texlFunction.MinArity ? requiredParams_ex : optionalParams_ex).Add(parameter_ex);
                }

                dynamic signature_ex = new ExpandoObject();
                signature_ex.RequiredParameters = requiredParams_ex;
                signature_ex.OptionalParameters = optionalParams_ex;

                signatures_ex.Add(signature_ex);
            }

            texlFunction_ex.SignatureCount = signatures.Length;
            texlFunction_ex.Signatures = signatures_ex;
            texlFunction_ex.ReturnType = texlFunction.ReturnType.ToString(); //  DType
            texlFunction_ex.RequiredEnumNames = string.Join(", ", texlFunction.GetRequiredEnumNames());

            texlFunction_ex.Capabilities = texlFunction.Capabilities.ToCapabilitiesString();
            texlFunction_ex.FunctionCategoriesMask = texlFunction.FunctionCategoriesMask.ToFunctionCategoriesString();
            texlFunction_ex.FunctionDelegationCapability = texlFunction.FunctionDelegationCapability.ToDelegationCapabilityString();
            texlFunction_ex.FunctionPermission = texlFunction.FunctionPermission.ToFunctionPermissionString();
            texlFunction_ex.ScopeInfo = texlFunction.ScopeInfo.ToExpando();

            texlFunction_ex.AffectsAliases = texlFunction.AffectsAliases;
            texlFunction_ex.AffectsCollectionSchemas = texlFunction.AffectsCollectionSchemas;
            texlFunction_ex.AffectsDataSourceQueryOptions = texlFunction.AffectsDataSourceQueryOptions;
            texlFunction_ex.AffectsScopeVariable = texlFunction.AffectsScopeVariable;
            texlFunction_ex.AllowedWithinNondeterministicOperationOrder = texlFunction.AllowedWithinNondeterministicOperationOrder;
            texlFunction_ex.CanReturnExpandInfo = texlFunction.CanReturnExpandInfo;
            texlFunction_ex.CanSuggestContextVariables = texlFunction.CanSuggestContextVariables;
            texlFunction_ex.CanSuggestInputColumns = texlFunction.CanSuggestInputColumns;
            texlFunction_ex.HasColumnIdentifiers = texlFunction.HasColumnIdentifiers;
            texlFunction_ex.HasEcsExcemptLambdas = texlFunction.HasEcsExcemptLambdas;
            texlFunction_ex.HasLambdas = texlFunction.HasLambdas;
            texlFunction_ex.HasPreciseErrors = texlFunction.HasPreciseErrors;
            texlFunction_ex.HelpLink = texlFunction.HelpLink;
            texlFunction_ex.IsAsync = texlFunction.IsAsync;
            texlFunction_ex.IsAutoRefreshable = texlFunction.IsAutoRefreshable;
            texlFunction_ex.IsBehaviorOnly = texlFunction.IsBehaviorOnly;
            texlFunction_ex.IsDeprecatedOrInternalFunction = texlFunction.IsDeprecatedOrInternalFunction;
            texlFunction_ex.IsDynamic = texlFunction.IsDynamic;
            texlFunction_ex.IsGlobalReliant = texlFunction.IsGlobalReliant;
            texlFunction_ex.IsHidden = texlFunction.IsHidden;
            texlFunction_ex.IsPure = texlFunction.IsPure;
            texlFunction_ex.IsSelfContained = texlFunction.IsSelfContained;
            texlFunction_ex.IsStateless = texlFunction.IsStateless;
            texlFunction_ex.IsStrict = texlFunction.IsStrict;
            texlFunction_ex.IsTestOnly = texlFunction.IsTestOnly;
            texlFunction_ex.IsVariadicFunction = texlFunction.IsVariadicFunction;
            texlFunction_ex.ManipulatesCollections = texlFunction.ManipulatesCollections;
            texlFunction_ex.ModifiesValues = texlFunction.ModifiesValues;
            texlFunction_ex.MutatesArg0 = texlFunction.MutatesArg0;
            texlFunction_ex.PropagatesMutability = texlFunction.PropagatesMutability;
            texlFunction_ex.RequireAllParamColumns = texlFunction.RequireAllParamColumns;
            texlFunction_ex.RequiresDataSourceScope = texlFunction.RequiresDataSourceScope;
            texlFunction_ex.ShowAIDisclaimer = texlFunction.ShowAIDisclaimer;
            texlFunction_ex.SignatureConstraint = texlFunction.SignatureConstraint.ToSignatureConstraint();
            texlFunction_ex.SkipScopeForInlineRecords = texlFunction.SkipScopeForInlineRecords;
            texlFunction_ex.SuggestionTypeReferenceParamIndex = texlFunction.SuggestionTypeReferenceParamIndex;
            texlFunction_ex.SupportsMetadataTypeArg = texlFunction.SupportsMetadataTypeArg;
            texlFunction_ex.SupportsParamCoercion = texlFunction.SupportsParamCoercion;
            texlFunction_ex.UseParentScopeForArgumentSuggestions = texlFunction.UseParentScopeForArgumentSuggestions;
            texlFunction_ex.UsesEnumNamespace = texlFunction.UsesEnumNamespace;

            return texlFunction_ex;
        }

        private static dynamic ToSignatureConstraint(this SignatureConstraint signatureConstraint)
        {
            dynamic sigConstraint_ex = new ExpandoObject();

            if (signatureConstraint == null)
            {
                return sigConstraint_ex;
            }

            sigConstraint_ex.EndNonRepeatCount = signatureConstraint.EndNonRepeatCount;
            sigConstraint_ex.OmitStartIndex = signatureConstraint.OmitStartIndex;
            sigConstraint_ex.RepeatSpan = signatureConstraint.RepeatSpan;
            sigConstraint_ex.RepeatTopLength = signatureConstraint.RepeatTopLength;

            return sigConstraint_ex;
        }

        private static ExpandoObject ToExpando(this FunctionScopeInfo scopeInfo)
        {
            dynamic sInfo_ex = new ExpandoObject();

            if (scopeInfo == null)
            {
                return sInfo_ex;
            }

            sInfo_ex.AcceptsLiteralPredicates = scopeInfo.AcceptsLiteralPredicates;
            sInfo_ex.CanBeCreatedByRecord = scopeInfo.CanBeCreatedByRecord;
            sInfo_ex.HasNondeterministicOperationOrder = scopeInfo.HasNondeterministicOperationOrder;
            sInfo_ex.IteratesOverScope = scopeInfo.IteratesOverScope;
            sInfo_ex.ScopeType = scopeInfo.ScopeType?.ToString() ?? "<null>"; // DType
            sInfo_ex.SupportsAsyncLambdas = scopeInfo.SupportsAsyncLambdas;
            sInfo_ex.UsesAllFieldsInScope = scopeInfo.UsesAllFieldsInScope;

            return sInfo_ex;
        }

        // Returns list of flags
        private static string ToFunctionPermissionString(this RequiredDataSourcePermissions requiredDataSourcePermissions)
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
        private static string ToDelegationCapabilityString(this DelegationCapability delegationCapability)
        {
            return $"0x{delegationCapability.Capabilities.ToString("X", CultureInfo.InvariantCulture)}";
        }

        // Returns list of flags in alpha order
        private static string ToFunctionCategoriesString(this FunctionCategories functionCategories)
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
        private static string ToCapabilitiesString(this Capabilities capabilities)
        {
            if (capabilities == 0u)
            {
                return nameof(Capabilities.None);
            }

            List<string> flagList = new List<string>();

            if (capabilities.HasFlag(Capabilities.OutboundInternetAccess))
            {
                flagList.Add(nameof(Capabilities.OutboundInternetAccess));
            }

            if (capabilities.HasFlag(Capabilities.PrivateNetworkAccess))
            {
                flagList.Add(nameof(Capabilities.PrivateNetworkAccess));
            }

            if (capabilities.HasFlag(Capabilities.EnterpriseAuthentication))
            {
                flagList.Add(nameof(Capabilities.EnterpriseAuthentication));
            }

            if (capabilities.HasFlag(Capabilities.LocalStateAccess))
            {
                flagList.Add(nameof(Capabilities.LocalStateAccess));
            }

            if (capabilities.HasFlag(Capabilities.PicturesLibraryAccess))
            {
                flagList.Add(nameof(Capabilities.PicturesLibraryAccess));
            }

            if (capabilities.HasFlag(Capabilities.VideoLibraryAccess))
            {
                flagList.Add(nameof(Capabilities.VideoLibraryAccess));
            }

            if (capabilities.HasFlag(Capabilities.MusicLibraryAccess))
            {
                flagList.Add(nameof(Capabilities.MusicLibraryAccess));
            }

            if (capabilities.HasFlag(Capabilities.Camera))
            {
                flagList.Add(nameof(Capabilities.Camera));
            }

            if (capabilities.HasFlag(Capabilities.Microphone))
            {
                flagList.Add(nameof(Capabilities.Microphone));
            }

            if (capabilities.HasFlag(Capabilities.Location))
            {
                flagList.Add(nameof(Capabilities.Location));
            }

            return string.Join(", ", flagList.OrderBy(x => x));
        }
    }
}
