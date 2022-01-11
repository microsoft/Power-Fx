// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // SortByColumns(source:*, name:s, order:s...name:s, [order:s])
    internal sealed class SortByColumnsFunction : BuiltinFunction
    {
        private readonly SortOrderValidator _sortOrderValidator;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public SortByColumnsFunction()
            : base("SortByColumns", TexlStrings.AboutSortByColumns, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable, DType.String)
        {
            _sortOrderValidator = ArgValidators.SortOrderValidator;

            // SortByColumns(source, name, order, name, order, ...name, order, ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 5, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 9);
        }

        public override bool RequiresErrorContext => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 2 possibilities).
            yield return new[] { TexlStrings.SortByColumnsArg1, TexlStrings.SortByColumnsArg2 };
            yield return new[] { TexlStrings.SortByColumnsArg1, TexlStrings.SortByColumnsArg2, TexlStrings.SortByColumnsArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetOverloadsSortByColumns(arity);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            returnType = argTypes[0];

            var sourceType = argTypes[0];
            for (var i = 1; i < args.Length; i += 2)
            {
                var colNameArg = args[i];
                var colNameArgType = argTypes[i];
                StrLitNode nameNode;

                if (colNameArgType.Kind != DKind.String)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, colNameArg, TexlStrings.ErrStringExpected);
                    fValid = false;
                }
                else if ((nameNode = colNameArg.AsStrLit()) != null)
                {
                    // Verify that the name is valid.
                    if (DName.IsValidDName(nameNode.Value))
                    {
                        var columnName = new DName(nameNode.Value);

                        // Verify that the name exists.
                        if (!sourceType.TryGetType(columnName, out var columnType))
                        {
                            sourceType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, args[i]);
                            fValid = false;
                        }
                        else if (!columnType.IsPrimitive || columnType.IsOptionSet)
                        {
                            fValid = false;
                            errors.EnsureError(colNameArg, TexlStrings.ErrSortWrongType);
                        }
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, nameNode, TexlStrings.ErrArgNotAValidIdentifier_Name, nameNode.Value);
                        fValid = false;
                    }
                }

                var nextArgIdx = i + 1;
                if (nextArgIdx < args.Length && argTypes[nextArgIdx] != DType.String)
                {
                    fValid = false;
                    errors.EnsureError(args[i + 1], TexlStrings.ErrSortIncorrectOrder);
                }
            }

            return fValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex >= 0;
        }

        private bool IsColumnSortable(StrLitNode node, TexlBinding binding, SortOpMetadata sortMetadata)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(sortMetadata);

            var columnPath = DPath.Root.Append(new DName(node.Value));
            if (!sortMetadata.IsDelegationSupportedByColumn(columnPath, DelegationCapability.Sort))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            return true;
        }

        private bool IsValidSortableColumnNode(TexlNode node, TexlBinding binding, SortOpMetadata metadata)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            if (binding.ErrorContainer.HasErrors(node) || node.Kind != NodeKind.StrLit)
            {
                return false;
            }

            var columnName = node.AsStrLit().VerifyValue();
            return IsColumnSortable(columnName, binding, metadata);
        }

        private bool IsValidSortOrderNode(TexlNode node, SortOpMetadata metadata, TexlBinding binding, DPath columnPath)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);
            Contracts.AssertValid(columnPath);

            if (binding.IsAsync(node))
            {
                var message = string.Format("Function:{0}, SortOrderNode is async", Name);
                AddSuggestionMessageToTelemetry(message, node, binding);
                return false;
            }

            string sortOrder;
            switch (node.Kind)
            {
                case NodeKind.FirstName:
                case NodeKind.StrLit:
                    return _sortOrderValidator.TryGetValidValue(node, binding, out sortOrder) &&
                        IsSortOrderSuppportedByColumn(sortOrder, metadata, columnPath);
                case NodeKind.DottedName:
                case NodeKind.Call:
                    if (_sortOrderValidator.TryGetValidValue(node, binding, out sortOrder) &&
                        IsSortOrderSuppportedByColumn(sortOrder, metadata, columnPath))
                    {
                        return true;
                    }

                    // If both ascending and descending are supported then we can support this.
                    return IsSortOrderSuppportedByColumn(LanguageConstants.DescendingSortOrderString, metadata, columnPath) &&
                        IsSortOrderSuppportedByColumn(LanguageConstants.AscendingSortOrderString, metadata, columnPath);
                default:
                    AddSuggestionMessageToTelemetry("Unsupported sortorder node.", node, binding);
                    return false;
            }
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (binding.ErrorContainer.HasErrors(callNode))
            {
                return false;
            }

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            SortOpMetadata metadata = null;
            if (TryGetEntityMetadata(callNode, binding, out IDelegationMetadata delegationMetadata))
            {
                if (!binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled ||
                    !TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
                {
                    SuggestDelegationHint(callNode, binding);
                    return false;
                }

                metadata = delegationMetadata.SortDelegationMetadata.VerifyValue();
            }
            else
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.Sort, out var dataSource))
                {
                    return false;
                }

                metadata = dataSource.DelegationMetadata.SortDelegationMetadata;
            }

            var args = callNode.Args.Children.VerifyValue();
            var cargs = args.Count();

            const string defaultSortOrder = LanguageConstants.AscendingSortOrderString;

            for (var i = 1; i < cargs; i += 2)
            {
                if (!IsValidSortableColumnNode(args[i], binding, metadata))
                {
                    SuggestDelegationHint(args[i], binding);
                    return false;
                }

                var columnName = args[i].AsStrLit().VerifyValue().Value;
                var sortOrderNode = (i + 1) < cargs ? args[i + 1] : null;
                var sortOrder = sortOrderNode == null ? defaultSortOrder : string.Empty;
                if (sortOrderNode != null)
                {
                    if (!IsValidSortOrderNode(sortOrderNode, metadata, binding, DPath.Root.Append(new DName(columnName))))
                    {
                        SuggestDelegationHint(sortOrderNode, binding);
                        return false;
                    }
                }
                else if (!IsSortOrderSuppportedByColumn(sortOrder, metadata, DPath.Root.Append(new DName(columnName))))
                {
                    SuggestDelegationHint(args[i], binding);
                    return false;
                }
            }

            return true;
        }

        private bool IsSortOrderSuppportedByColumn(string order, SortOpMetadata metadata, DPath columnPath)
        {
            Contracts.AssertValue(order);
            Contracts.AssertValue(metadata);
            Contracts.AssertValid(columnPath);

            order = order.ToLower();

            // If column is marked as ascending only then return false if order requested is descending.
            return order != LanguageConstants.DescendingSortOrderString || !metadata.IsColumnAscendingOnly(columnPath);
        }

        // Gets the overloads for SortByColumns function for the specified arity.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsSortByColumns(int arity)
        {
            Contracts.Assert(arity > 3);

            const int OverloadCount = 2;

            var overloads = new List<TexlStrings.StringGetter[]>(OverloadCount);

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength : arity;
            for (var ioverload = 0; ioverload < OverloadCount; ioverload++)
            {
                var iArgCount = argCount + ioverload;
                var overload = new TexlStrings.StringGetter[iArgCount];
                overload[0] = TexlStrings.SortByColumnsArg1;
                for (var iarg = 1; iarg < iArgCount; iarg += 2)
                {
                    overload[iarg] = TexlStrings.SortByColumnsArg2;

                    if (iarg < iArgCount - 1)
                    {
                        overload[iarg + 1] = TexlStrings.SortByColumnsArg3;
                    }
                }

                overloads.Add(overload);
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(overloads);
        }

        public override bool AffectsDataSourceQueryOptions => true;

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding, DocumentErrorSeverity.Moderate))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();

            var dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            var retval = false;

            for (var i = 1; i < args.Length; i += 2)
            {
                var columnType = binding.GetType(args[i]);
                var columnNode = args[i].AsStrLit();
                if (columnType.Kind != DKind.String || columnNode == null)
                {
                    continue;
                }

                var columnName = columnNode.Value;

                Contracts.Assert(dsType.Contains(new DName(columnName)));

                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }
    }

    // SortByColumns(source:*, name:s, values:*[])
    internal sealed class SortByColumnsOrderTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public SortByColumnsOrderTableFunction()
            : base("SortByColumns", TexlStrings.AboutSortByColumnsWithOrderValues, FunctionCategories.Table, DType.EmptyTable, 0, 3, 3, DType.EmptyTable, DType.String, DType.EmptyTable)
        {
        }

        public override bool RequiresErrorContext => true;

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "OrderTable");
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SortByColumnsWithOrderValuesArg1, TexlStrings.SortByColumnsWithOrderValuesArg2, TexlStrings.SortByColumnsWithOrderValuesArg3 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            returnType = argTypes[0];
            var sourceType = argTypes[0];
            var nameArg = args[1];
            var nameArgType = argTypes[1];
            StrLitNode nameNode = null;
            var columnType = DType.Invalid;

            if (nameArgType.Kind != DKind.String)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrStringExpected);
                fValid = false;
            }
            else if ((nameNode = nameArg.AsStrLit()) != null)
            {
                // Verify that the name is valid.
                if (DName.IsValidDName(nameNode.Value))
                {
                    var columnName = new DName(nameNode.Value);

                    // Verify that the name exists.
                    if (!sourceType.TryGetType(columnName, out columnType))
                    {
                        sourceType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, nameNode);
                        fValid = false;
                    }
                    else if (!columnType.IsPrimitive)
                    {
                        fValid = false;
                        errors.EnsureError(nameArg, TexlStrings.ErrSortWrongType);
                    }
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameNode, TexlStrings.ErrArgNotAValidIdentifier_Name, nameNode.Value);
                    fValid = false;
                }
            }

            var valuesArg = args[2];
            IEnumerable<TypedName> columns;
            if ((columns = argTypes[2].GetNames(DPath.Root)).Count() != 1)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, valuesArg, TexlStrings.ErrInvalidSchemaNeedCol);
                return false;
            }

            var column = columns.Single();
            if (nameNode != null && columnType.IsValid && !columnType.Accepts(column.Type))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, valuesArg, TexlStrings.ErrTypeError_Arg_Expected_Found, nameNode.Value,
                    columnType.GetKindString(), column.Type.GetKindString());
                fValid = false;
            }

            return fValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0 || argumentIndex == 1;
        }

        public override bool AffectsDataSourceQueryOptions => true;

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            // Ignore delegation warning
            if (!CheckArgsCount(callNode, binding, DocumentErrorSeverity.Moderate))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();

            var dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            var columnType = binding.GetType(args[1]);
            var columnNode = args[1].AsStrLit();
            if (columnType.Kind != DKind.String || columnNode == null)
            {
                return false;
            }

            var columnName = columnNode.Value;

            Contracts.Assert(dsType.Contains(new DName(columnName)));

            return dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
        }
    }
}
