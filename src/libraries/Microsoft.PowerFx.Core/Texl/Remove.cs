// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Remove(collection:*[], item1:![], item2:![], ..., ["All"])
    internal class RemoveFunction : BuiltinFunction, ISuggestionAwareFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool CanSuggestInputColumns => true;

        public override bool IsSelfContained => false;

        public bool CanSuggestThisItem => true;

        public override bool RequiresDataSourceScope => true;

        public override bool SupportsParamCoercion => false;

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Delete;

        // Return true if this function affects datasource query options.
        public override bool AffectsDataSourceQueryOptions => true;

        public override bool MutatesArg(int argIndex, TexlNode arg) => argIndex == 0;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex > 0)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public RemoveFunction()
            : base("Remove", TexlStrings.AboutRemove, FunctionCategories.Behavior, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RemoveArg1, TexlStrings.RemoveArg2 };
            yield return new[] { TexlStrings.RemoveArg1, TexlStrings.RemoveArg2, TexlStrings.RemoveArg2 };
            yield return new[] { TexlStrings.RemoveArg1, TexlStrings.RemoveArg2, TexlStrings.RemoveArg2, TexlStrings.RemoveArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.RemoveArg1, TexlStrings.RemoveArg2);
            }

            return base.GetSignatures(arity);
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.RemoveFlagsEnumString };
        }

        public override bool IsLazyEvalParam(TexlNode node, int index, Features features)
        {
            // First argument to mutation functions is Lazy for datasources that are copy-on-write.
            // If there are any side effects in the arguments, we want those to have taken place before we make the copy.
            return index == 0;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                fValid = false;
                errors.EnsureError(args[0], TexlStrings.ErrNeedCollection_Func, Name);
            }

            int argCount = argTypes.Length;
            for (int i = 1; i < argCount; i++)
            {
                DType argType = argTypes[i];

                if (!argType.IsRecord)
                {
                    if (argCount >= 3 && i == argCount - 1)
                    {
                        if (context.AnalysisMode)
                        {
                            if (!DType.String.Accepts(argType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) &&
                                !BuiltInEnums.RemoveFlagsEnum.FormulaType._type.Accepts(argTypes[i], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                            {
                                fValid = false;
                                errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrRemoveAllArg);
                            }
                        }
                        else
                        {
                            if (!BuiltInEnums.RemoveFlagsEnum.FormulaType._type.Accepts(argTypes[i], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                            {
                                fValid = false;
                                errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrRemoveAllArg);
                            }
                        }

                        continue;
                    }
                    else
                    {
                        fValid = false;
                        errors.EnsureError(args[i], TexlStrings.ErrNeedRecord_Arg, args[i]);
                        continue;
                    }
                }

                var collectionAcceptsRecord = collectionType.Accepts(argType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
                var recordAcceptsCollection = argType.ToTable().Accepts(collectionType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);

                // PFxV1 is more restrictive than PA in terms of column matching. If the collection does not accept the record or vice versa, it is an error.
                // The item schema should be compatible with the collection schema.
                if ((context.Features.PowerFxV1CompatibilityRules && (!collectionAcceptsRecord || !recordAcceptsCollection)) ||
                    (!context.Features.PowerFxV1CompatibilityRules && (!collectionAcceptsRecord && !recordAcceptsCollection)))
                {
                    fValid = false;
                    SetErrorForMismatchedColumns(collectionType, argType, args[i], errors, context.Features);
                }

                // Only warn about no-op record inputs if there are no data sources that would use reference identity for comparison.
                else if (!collectionType.AssociatedDataSources.Any() && !recordAcceptsCollection)
                {
                    errors.EnsureError(DocumentErrorSeverity.Warning, args[i], TexlStrings.ErrCollectionDoesNotAcceptThisType);
                }

                if (!context.AnalysisMode)
                {
                    // ArgType[N] (0<N<argCount) must match all the fields with the data source.
                    bool checkAggregateNames = argType.CheckAggregateNames(collectionType, args[i], errors, context.Features, SupportsParamCoercion);

                    // The item schema should be compatible with the collection schema.
                    if (!checkAggregateNames)
                    {
                        fValid = false;
                        if (!SetErrorForMismatchedColumns(collectionType, argType, args[i], errors, context.Features))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTableDoesNotAcceptThisType);
                        }
                    }
                }
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
            MutationUtils.CheckSemantics(binding, this, args, argTypes, errors);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex != 1;
        }

        public override IEnumerable<Identifier> GetIdentifierOfModifiedValue(TexlNode[] args, out TexlNode identifierNode)
        {
            Contracts.AssertValue(args);

            identifierNode = null;
            if (args.Length == 0)
            {
                return null;
            }

            var firstNameNode = args[0]?.AsFirstName();
            identifierNode = firstNameNode;
            if (firstNameNode == null)
            {
                return null;
            }

            var identifiers = new List<Identifier>
            {
                firstNameNode.Ident
            };
            return identifiers;
        }

        /// <summary>
        /// As Remove uses the source record in it's entirity to find the entry in table, uses deepcompare at runtime, we need all fields from source.
        /// So update the selects for all columns in the source in this case except when datasource is pageable.
        /// In that case, we can get the info at runtime.
        /// </summary>
        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            var args = Contracts.VerifyValue(callNode.Args.Children);

            DType dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null
                || dsType == DType.EmptyTable)
            {
                return false;
            }

            var sourceRecordType = binding.GetType(args[1]);

            // This might be the case where Remove(CDS, Gallery.Selected)
            if (sourceRecordType == DType.EmptyRecord)
            {
                return false;
            }

            var firstTypeName = sourceRecordType.GetNames(DPath.Root).FirstOrDefault();

            if (!firstTypeName.IsValid)
            {
                return false;
            }

            DType type = firstTypeName.Type;
            DName columnName = firstTypeName.Name;

            // This might be the case where Remove(CDS, Gallery.Selected)
            if (!dsType.Contains(columnName))
            {
                return false;
            }

            dsType.AssociateDataSourcesToSelect(
                dataSourceToQueryOptionsMap,
                columnName,
                type,
                false /*skipIfNotInSchema*/,
                true); /*skipExpands*/

            return true;
        }

        // This method filters for a table as the first parameter, records as intermediate parameters
        // and a string (First/All) as the final parameter.
        public override bool IsSuggestionTypeValid(int paramIndex, DType type)
        {
            Contracts.Assert(paramIndex >= 0);
            Contracts.AssertValid(type);

            if (paramIndex >= MaxArity)
            {
                return false;
            }

            if (paramIndex == 0)
            {
                return type.IsTable;
            }

            // String suggestions for column names may occur within the context of a record
            return type.IsRecord || type.Kind == DKind.String;
        }

        public override bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        protected override bool RequiresPagedDataForParamCore(TexlNode[] args, int paramIndex, TexlBinding binding)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.Assert(paramIndex >= 0 && paramIndex < args.Length);
            Contracts.AssertValue(binding);
            Contracts.Assert(binding.IsPageable(Contracts.VerifyValue(args[paramIndex])));

            // For the first argument, we need only metadata. No actual data from datasource is required.
            return paramIndex > 0;
        }
    }

    // Remove(collection:*[], source:*[], ["All"])
    internal class RemoveAllFunction : BuiltinFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        public override bool SupportsParamCoercion => false;

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Delete;

        public override bool MutatesArg(int argIndex, TexlNode arg) => argIndex == 0;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum == 1;
        }

        public RemoveAllFunction()
            : base("Remove", TexlStrings.AboutRemove, FunctionCategories.Behavior, DType.EmptyTable, 0, 2, 3, DType.EmptyTable, DType.EmptyTable, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RemoveArg1, TexlStrings.RemoveAllArg2 };
            yield return new[] { TexlStrings.RemoveArg1, TexlStrings.RemoveAllArg2, TexlStrings.RemoveArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.RemoveFlagsEnumString };
        }

        public override bool IsLazyEvalParam(TexlNode node, int index, Features features)
        {
            // First argument to mutation functions is Lazy for datasources that are copy-on-write.
            // If there are any side effects in the arguments, we want those to have taken place before we make the copy.
            return index == 0;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                fValid = false;
                errors.EnsureError(args[0], TexlStrings.ErrNeedTable_Func, Name);
            }

            // The source to be collected must be a table.
            DType sourceType = argTypes[1];
            if (!sourceType.IsTable)
            {
                fValid = false;
                errors.EnsureError(args[1], TexlStrings.ErrNeedTable_Arg, args[1]);
            }

            if (!context.AnalysisMode)
            {
                bool checkAggregateNames = sourceType.CheckAggregateNames(collectionType, args[1], errors, context.Features, SupportsParamCoercion);

                // The item schema should be compatible with the collection schema.
                if (!checkAggregateNames)
                {
                    fValid = false;
                    if (!SetErrorForMismatchedColumns(collectionType, sourceType, args[1], errors, context.Features))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTableDoesNotAcceptThisType);
                    }
                }
            }

            // The source schema should be compatible with the collection schema.
            else if (!collectionType.Accepts(sourceType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) && !sourceType.Accepts(collectionType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                fValid = false;
                if (!SetErrorForMismatchedColumns(collectionType, sourceType, args[1], errors, context.Features))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrCollectionDoesNotAcceptThisType);
                }
            }

            if (args.Length == 3)
            {
                if (context.AnalysisMode)
                {
                    if (!DType.String.Accepts(argTypes[2], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) &&
                        !BuiltInEnums.RemoveFlagsEnum.FormulaType._type.Accepts(argTypes[2], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrRemoveAllArg);
                    }
                }
                else
                {
                    if (!BuiltInEnums.RemoveFlagsEnum.FormulaType._type.Accepts(argTypes[2], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrRemoveAllArg);
                    }
                }
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
            MutationUtils.CheckSemantics(binding, this, args, argTypes, errors);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        public override IEnumerable<Identifier> GetIdentifierOfModifiedValue(TexlNode[] args, out TexlNode identifierNode)
        {
            Contracts.AssertValue(args);

            identifierNode = null;
            if (args.Length == 0)
            {
                return null;
            }

            var firstNameNode = args[0]?.AsFirstName();
            identifierNode = firstNameNode;
            if (firstNameNode == null)
            {
                return null;
            }

            var identifiers = new List<Identifier>
            {
                firstNameNode.Ident
            };
            return identifiers;
        }

        public override bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            // Use ECS flag as a guard.
            if (!binding.Features.IsRemoveAllDelegationEnabled)
            {
                return false;
            }

            if (!binding.TryGetDataSourceInfo(callNode.Args.Children[0], out IExternalDataSource dataSource))
            {
                return false;
            }

            // Currently we delegate only to CDS. This is the first version of the feature and not a limitation of other datasources
            if (dataSource == null || dataSource?.Kind != DataSourceKind.CdsNative)
            {
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.DataSourceNotDelegatable, callNode, binding, this);
                return false;
            }

            // Right now we delegate only if the set of records is a table/queried table to mitigate the performance impact of the remove operation.
            // Deleting single records (via Lookup) does not have the same performance impact
            var dsType = binding.GetType(callNode.Args.Children[1]).Kind;
            if (dsType != DKind.Table)
            {
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.InvalidArgType, callNode, binding, this);
                return false;
            }

            TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.DelegationSuccessful, callNode, binding, this);
            return true;
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            if (!binding.TryGetDataSourceInfo(callNode.Args.Children[0], out IExternalDataSource dataSource))
            {
                return false;
            }

            // Currently we delegate only to CDS. This is the first version of the feature and not a limitation of other datasources
            if (dataSource == null || dataSource?.Kind == DataSourceKind.CdsNative)
            {
                return false;
            }

            return base.SupportsPaging(callNode, binding);
        }
    }
}
