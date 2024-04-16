// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    #region Base classes
    internal abstract class PatchAndValidateRecordFunctionBase : FunctionWithTableInput
    {
        public PatchAndValidateRecordFunctionBase(DPath theNamespace, string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(theNamespace, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public PatchAndValidateRecordFunctionBase(string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool RequiresDataSourceScope => true;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        private DType ExpandMetaFieldType(DType metaFieldType)
        {
            Contracts.AssertValid(metaFieldType);
            Contracts.Assert(metaFieldType.HasMetaField());

            DType curType = metaFieldType;
            foreach (var typedName in metaFieldType.GetNames(DPath.Root))
            {
                if (!curType.TryGetType(typedName.Name, out DType unusedType))
                {
                    curType = curType.Add(typedName);
                }
            }

            return curType;
        }

        protected bool CheckTypesCore(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap, bool expectsTableArgs = false)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            bool isValid = true;
            DType dataSourceType = argTypes[0];
            DType retType = expectsTableArgs ? DType.EmptyTable : DType.EmptyRecord;
            foreach (var assocDS in dataSourceType.AssociatedDataSources)
            {
                retType = DType.AttachDataSourceInfo(retType, assocDS);
            }

            if (dataSourceType.DisplayNameProvider != null)
            {
                retType = DType.AttachOrDisableDisplayNameProvider(retType, dataSourceType.DisplayNameProvider);
            }

            bool sourceContainsControlType = dataSourceType.ContainsControlType(DPath.Root);

            for (int i = 1; i < args.Length; i++)
            {
                DType curType = argTypes[i];
                if (expectsTableArgs ? !curType.IsTable : !curType.IsRecord)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedRecord_Arg, args[i]);
                    isValid = false;
                    continue;
                }

                // Ensure that if the key in argument1 exists in the current record, their types match.
                bool isSafeToUnion = true;

                if (context.Features.PowerFxV1CompatibilityRules)
                {
                    isValid = isSafeToUnion = curType.CheckAggregateNames(dataSourceType, args[i], errors, context.Features, SupportsParamCoercion);
                }
                else
                {
                    bool hasControlFieldType = false;
                    foreach (var typedName in curType.GetNames(DPath.Root))
                    {
                        // As long as source doesn't contain control type we can skip the type check for control types.
                        if (!sourceContainsControlType && typedName.Type.IsControl)
                        {
                            hasControlFieldType = true;
                            continue;
                        }

                        // If the datasource doesn't contain the supplied type.
                        DName name = typedName.Name;
                        if (!dataSourceType.TryGetType(name, out DType dsNameType))
                        {
                            dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, name, args[i]);
                            isValid = isSafeToUnion = false;
                            continue;
                        }

                        DType type = typedName.Type;

                        // If the type has metafield in it then it's coming from a control type like dropdown or listbox.
                        // For example, dropdown.SelectedItems, listbox.SelectedItems. So expand the type to include property types as well.
                        // In Document.cs!AugmentedExpandoType function, we don't add the field to the expandotype if it conflicts with any expando property.
                        // So the type doesn't include that field. This logic tries to rectify that by adding the missing fields back.
                        // This is only necessary if we're trying to compare it to an entity or aggregate type, as the meta field will expand to a table or record
                        if (type.HasMetaField() && (dsNameType.IsAggregate || dsNameType.Kind == DKind.DataEntity) && type.IsAggregate)
                        {
                            type = ExpandMetaFieldType(type);
                        }

                        // For patching entities, we expand the type and drop entities and attachments for the purpose of comparison.
                        if (dsNameType.Kind == DKind.DataEntity && type.Kind != DKind.DataEntity)
                        {
                            if (dsNameType.TryGetExpandedEntityTypeWithoutDataSourceSpecificColumns(out DType expandedType))
                            {
                                dsNameType = expandedType;
                            }
                        }

                        if (!dsNameType.Accepts(type, out var schemaDifference, out var schemaDifferenceType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) &&
                            (!SupportsParamCoercion || !type.CoercesTo(dsNameType, out bool coercionIsSafe, aggregateCoercion: false, isTopLevelCoercion: false, context.Features) || !coercionIsSafe))
                        {
                            if (dsNameType.Kind == type.Kind)
                            {
                                errors.Errors(args[i], type, schemaDifference, schemaDifferenceType);
                            }
                            else
                            {
                                errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTypeError_Arg_Expected_Found, name, dsNameType.GetKindString(), type.GetKindString());
                            }

                            isValid = isSafeToUnion = false;
                        }
                    }

                    if (hasControlFieldType)
                    {
                        bool fError = false;
                        curType = curType.DropAllOfKind(ref fError, DPath.Root, DKind.Control);
                        if (fError)
                        {
                            isValid = isSafeToUnion = false;
                        }
                    }
                }

                if (isValid && SupportsParamCoercion && !dataSourceType.Accepts(curType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    if (!curType.TryGetCoercionSubType(dataSourceType, out DType coercionType, out bool coercionNeeded, context.Features))
                    {
                        isValid = false;
                    }
                    else
                    {
                        if (coercionNeeded)
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                        }

                        retType = DType.Union(retType, coercionType, useLegacyDateTimeAccepts: false, context.Features);
                    }
                }
                else if (isSafeToUnion)
                {
                    retType = DType.Union(retType, curType, useLegacyDateTimeAccepts: false, context.Features);
                }
            }

            returnType = retType;
            return isValid;
        }

        protected void CheckSemanticsCore(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, bool expectsTableArgs = false)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            DType dataSourceType = argTypes[0];
            DType retType = expectsTableArgs ? DType.EmptyTable : DType.EmptyRecord;

            if (binding.Features.PowerFxV1CompatibilityRules && dataSourceType.IsTable && dataSourceType.Kind != DKind.ObjNull)
            {
                base.ValidateArgumentIsMutable(binding, args[0], errors);
            }

            foreach (var assocDS in dataSourceType.AssociatedDataSources)
            {
                retType = DType.AttachDataSourceInfo(retType, assocDS);
            }

            bool sourceContainsControlType = dataSourceType.ContainsControlType(DPath.Root);

            for (int i = 1; i < args.Length; i++)
            {
                DType curType = argTypes[i];
                if (expectsTableArgs ? !curType.IsTable : !curType.IsRecord)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedRecord_Arg, args[i]);
                    continue;
                }

                // Ensure that if the key in argument1 exists in the current record, their types match.
                foreach (var typedName in curType.GetNames(DPath.Root))
                {
                    // As long as source doesn't contain control type we can skip the type check for control types.
                    if (!sourceContainsControlType && typedName.Type.IsControl)
                    {
                        continue;
                    }

                    // If the datasource doesn't contain the supplied type.
                    DName name = typedName.Name;
                    if (!dataSourceType.TryGetType(name, out DType dsNameType))
                    {
                        dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, name, args[i]);
                        continue;
                    }

                    DType type = typedName.Type;

                    // If the type has metafield in it then it's coming from a control type like dropdown or listbox.
                    // For example, dropdown.SelectedItems, listbox.SelectedItems. So expand the type to include property types as well.
                    // In Document.cs!AugmentedExpandoType function, we don't add the field to the expandotype if it conflicts with any expando property.
                    // So the type doesn't include that field. This logic tries to rectify that by adding the missing fields back.
                    // This is only necessary if we're trying to compare it to an entity or aggregate type, as the meta field will expand to a table or record
                    if (type.HasMetaField() && (dsNameType.IsAggregate || dsNameType.Kind == DKind.DataEntity) && type.IsAggregate)
                    {
                        type = ExpandMetaFieldType(type);
                    }

                    // For patching entities, we expand the type and drop entities and attachments for the purpose of comparison.
                    if (dsNameType.Kind == DKind.DataEntity && type.Kind != DKind.DataEntity)
                    {
                        if (!dsNameType.TryGetExpandedEntityTypeWithoutDataSourceSpecificColumns(out _))
                        {
                            binding.DeclareMetadataNeeded(dsNameType);
                        }
                    }
                }
            }
        }
    }    

    internal abstract class PatchAsyncFunctionCore : PatchAndValidateRecordFunctionBase
    {
        public override bool IsAsync => true;

        public override bool ManipulatesCollections => true;

        public override bool IsSelfContained => false;

        public override bool SupportsParamCoercion => true;

        public virtual bool ExpectsTableArgs => false;

        // Return true if this function affects datasource query options.
        public override bool AffectsDataSourceQueryOptions => true;

        public override bool MutatesArg0 => true;

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Create | RequiredDataSourcePermissions.Update;

        public PatchAsyncFunctionCore(string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            bool isValid = base.CheckTypes(context, args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            // We are going to discard the returnType infered by base.CheckTypes.
            // Use DType.Error until we can correctly infer the return type.
            returnType = DType.Error;
            if (!isValid)
            {
                return false;
            }

            return CheckTypesCore(context, args, argTypes, errors, out returnType, ref nodeToCoercedTypeMap, expectsTableArgs: ExpectsTableArgs);
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            CheckSemanticsCore(binding, args, argTypes, errors, ExpectsTableArgs);
            MutationUtils.CheckSemantics(binding, this, args, argTypes, errors);
        }

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
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            // !!!TODO : Defaults has not yet been implemented in Core. We need to revisit this code when it is implemented.
            //// When using Defaults() we add the first item in the schema just to ensure that the call is made
            //if (binding.TryGetCall(args[1].Id, out var recordArg) && recordArg.Function == BuiltinFunctions.Defaults)
            //{
            //    var recordArgType = binding.GetType(args[1]);
            //    var firstTypeName = recordArgType.GetNames(DPath.Root).FirstOrDefault();
            //    if (firstTypeName != null)
            //    {
            //        DName columnName = firstTypeName.Name;

            //        if (columnName.IsValid && dsType.Contains(columnName))
            //        {
            //            dsType.AssociateDataSourcesToSelect(
            //                dataSourceToQueryOptionsMap,
            //                columnName,
            //                firstTypeName.Type,
            //                false /*skipIfNotInSchema*/,
            //                true); /*skipExpands*/
            //        }
            //    }
            //}

            // Start from third patch argument to collect selects as
            // first argument is datasource and second argument is where clause in data call to update.
            for (var i = 2; i < args.Count; i++)
            {
                var recordType = binding.GetType(args[i]);

                foreach (var typeName in recordType.GetNames(DPath.Root))
                {
                    DType type = typeName.Type;
                    DName columnName = typeName.Name;

                    if (!dsType.Contains(columnName))
                    {
                        continue;
                    }

                    foreach (var tabularDataSource in dsType.AssociatedDataSources)
                    {
                        dataSourceToQueryOptionsMap.AddSelect(tabularDataSource, columnName);
                    }
                }
            }

            return true;
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
    #endregion

    #region Overloads classes

    // Patch(dataSource:*[], Record, Updates1, Updates2,…)
    internal class PatchFunction : PatchAsyncFunctionCore
    {
        public override bool CanSuggestInputColumns => true;

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex > 1)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public PatchFunction()
            : base("Patch", TexlStrings.AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 3, int.MaxValue, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update };
            yield return new[] { TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update, TexlStrings.PatchArg_Update };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update);
            }

            return base.GetSignatures(arity);
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);

            if (binding.Features.PowerFxV1CompatibilityRules)
            {
                MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(2).ToArray(), argTypes.Skip(2).ToArray(), errors);
            }
        }
    }

    // Patch(DS, record_with_keys_and_updates)
    internal class PatchSingleRecordFunction : PatchAsyncFunctionCore
    {
        public override bool CanSuggestInputColumns => true;

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex == 1)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public PatchSingleRecordFunction()
            : base("Patch", TexlStrings.AboutPatchSingleRecord, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, 2, DType.EmptyTable, DType.EmptyRecord)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Record };
        }
    }

    // Patch(DS, table_of_rows, table_of_updates)
    internal class PatchAggregateFunction : PatchAsyncFunctionCore
    {
        public override bool ExpectsTableArgs => true;

        public PatchAggregateFunction()
            : base("Patch", TexlStrings.AboutPatchAggregate, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyTable, 0, 3, 3, DType.EmptyTable, DType.EmptyTable, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Rows, TexlStrings.PatchArg_Updates };
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);

            if (binding.Features.PowerFxV1CompatibilityRules)
            {
                MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(2).ToArray(), argTypes.Skip(2).ToArray(), errors);
            }
        }
    }

    // Patch(DS, table_of_rows_with_updates)
    internal class PatchAggregateSingleTableFunction : PatchAsyncFunctionCore
    {
        public override bool ExpectsTableArgs => true;

        public PatchAggregateSingleTableFunction()
            : base("Patch", TexlStrings.AboutPatchAggregateSingleTable, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyTable, 0, 2, 2, DType.EmptyTable, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Rows };
        }
    }

    // Patch(Record, Updates1, Updates2,…)
    internal class PatchRecordFunction : BuiltinFunction
    {
        public override bool CanSuggestInputColumns => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex > 0)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public PatchRecordFunction()
            : base("Patch", TexlStrings.AboutPatchRecord, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, int.MaxValue, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update };
            yield return new[] { TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update, TexlStrings.PatchArg_Update };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.PatchArg_Source, TexlStrings.PatchArg_Record, TexlStrings.PatchArg_Update);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            bool isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // We are going to discard the returnType infered by base.CheckTypes.
            // Use DType.Error until we can correctly infer the return type.
            returnType = DType.Error;

            if (!isValid)
            {
                return false;
            }

            DType recordType = argTypes[0];
            DType retType = recordType;
            for (int i = 1; i < args.Length; i++)
            {
                DType curType = argTypes[i];
                if (!curType.IsRecord)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedRecord_Arg, args[i]);
                    isValid = false;
                    continue;
                }

                // Ensure that if the key in argument1 exists in the current record, their types match.
                bool isSafeToUnion = true;
                foreach (var typedName in curType.GetNames(DPath.Root))
                {
                    DName name = typedName.Name;
                    if (recordType.TryGetType(name, out DType nameType) && !nameType.Accepts(typedName.Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        errors.EnsureError(args[i], TexlStrings.ErrTypeError_Arg_Expected_Found, name, nameType.GetKindString(), typedName.Type.GetKindString());
                        isValid = isSafeToUnion = false;
                    }
                }

                if (isSafeToUnion)
                {
                    retType = DType.Union(retType, curType, useLegacyDateTimeAccepts: false, context.Features);
                }
            }

            returnType = retType;
            return isValid;
        }
    }

    #endregion
}
