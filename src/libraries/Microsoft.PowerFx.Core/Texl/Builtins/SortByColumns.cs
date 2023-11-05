// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // SortByColumns(source:*, name:s, order:s...name:s, [order:s])
    // SortByColumns(source:*, name:s, values:*[])
    internal sealed class SortByColumnsFunction : BuiltinFunction
    {
        private readonly SortOrderValidator _sortOrderValidator;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasColumnIdentifiers => true;

        public SortByColumnsFunction()
            : base("SortByColumns", TexlStrings.AboutSortByColumns, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable, DType.String)
        {
            _sortOrderValidator = ArgValidators.SortOrderValidator;

            // SortByColumns(source, name, order, name, order, ...name, order, ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 5, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 9);

            ScopeInfo = new FunctionScopeInfo(this, appliesToArgument: (index) => (index % 2 == 1));
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 2 possibilities).
            yield return new[] { TexlStrings.SortByColumnsArg1, TexlStrings.SortByColumnsArg2 };
            yield return new[] { TexlStrings.SortByColumnsArg1, TexlStrings.SortByColumnsArg2, TexlStrings.SortByColumnsArg3 };
            yield return new[] { TexlStrings.SortByColumnsArg1, TexlStrings.SortByColumnsArg2, TexlStrings.SortByColumnsWithOrderValuesArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.SortOrderEnumString };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetOverloadsSortByColumns(arity);
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
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var orderExpectedType = context.Features.StronglyTypedBuiltinEnums ?
                BuiltInEnums.SortOrderEnum.FormulaType._type :
                DType.String;

            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;
            returnType = argTypes[0];
            var sourceType = argTypes[0];
            var isOrderTableOverload = fValid && args.Length == 3 && argTypes[2].IsTable;
            if (isOrderTableOverload)
            {
                return CheckTypesOrderTableOverload(context, args, argTypes, errors, sourceType, supportColumnNamesAsIdentifiers);
            }

            for (var i = 1; i < args.Length; i += 2)
            {
                var colNameArg = args[i];
                var colNameArgType = argTypes[i];
                DName columnName = default;
                DType columnType = null;

                if (supportColumnNamesAsIdentifiers)
                {
                    if (colNameArg is not FirstNameNode identifierNode)
                    {
                        // Argument '{0}' is invalid, expected an identifier.
                        errors.EnsureError(DocumentErrorSeverity.Severe, colNameArg, TexlStrings.ErrExpectedIdentifierArg_Name, colNameArg.ToString());
                        fValid = false;
                        continue;
                    }

                    columnName = identifierNode.Ident.Name;

                    // Verify that the name exists.
                    if (!sourceType.TryGetType(columnName, out columnType))
                    {
                        sourceType.ReportNonExistingName(FieldNameKind.Display, errors, columnName, args[i]);
                        fValid = false;
                    }
                }
                else
                {
                    if (colNameArgType.Kind != DKind.String)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, colNameArg, TexlStrings.ErrStringExpected);
                        fValid = false;
                        continue;
                    }

                    var nameNode = colNameArg.AsStrLit();
                    if (nameNode != null)
                    {
                        if (!DName.IsValidDName(nameNode.Value))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, nameNode, TexlStrings.ErrArgNotAValidIdentifier_Name, nameNode.Value);
                            fValid = false;
                            continue;
                        }

                        columnName = new DName(nameNode.Value);
                    }
                    else
                    {
                        if (context.Features.PowerFxV1CompatibilityRules)
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, colNameArg, TexlStrings.ErrExpectedStringLiteralArg_Name, colNameArg.ToString());
                            return false;
                        }
                        else
                        {
                            // Legacy behavior: it's ok for the column name not to be a constant string - no validation will be performed.
                        }
                    }
                }

                if (columnName.IsValid)
                {
                    // Verify that the name exists.
                    if (!sourceType.TryGetType(columnName, out columnType))
                    {
                        sourceType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, args[i]);
                        fValid = false;
                    }
                    else if (!columnType.IsPrimitive)
                    {
                        fValid = false;
                        errors.EnsureError(colNameArg, TexlStrings.ErrSortWrongType);
                    }
                }

                var nextArgIdx = i + 1;
                if (nextArgIdx < args.Length)
                {
                    if (!orderExpectedType.Accepts(argTypes[nextArgIdx], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        fValid = false;
                        errors.TypeMismatchError(args[i + 1], argTypes[nextArgIdx], argTypes[2]);
                    }
                    else if (orderExpectedType.OptionSetInfo is EnumSymbol enumSymbol1)
                    {
                        // For implementations, coerce enum option set values to the backing type
                        var coercionType = enumSymbol1.EnumType.GetEnumSupertype();
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i + 1], coercionType);
                    }
                }
            }

            return fValid;
        }

        private bool CheckTypesOrderTableOverload(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, DType sourceType, bool supportColumnNamesAsIdentifiers)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var nameArg = args[1];
            var nameArgType = argTypes[1];
            DName columnName = default;

            if (supportColumnNamesAsIdentifiers)
            {
                if (nameArg is not FirstNameNode identifierNode)
                {
                    // Argument '{0}' is invalid, expected an identifier.
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrExpectedIdentifierArg_Name, nameArg.ToString());
                    return false;
                }

                columnName = identifierNode.Ident.Name;
            }
            else
            {
                if (nameArgType.Kind != DKind.String)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrStringExpected);
                    return false;
                }

                StrLitNode nameNode = nameArg.AsStrLit();
                if (nameNode != null)
                {
                    // Verify that the name is valid.
                    if (!DName.IsValidDName(nameNode.Value))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, nameNode, TexlStrings.ErrArgNotAValidIdentifier_Name, nameNode.Value);
                        return false;
                    }

                    columnName = new DName(nameNode.Value);
                }
                else
                {
                    if (context.Features.PowerFxV1CompatibilityRules)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrExpectedStringLiteralArg_Name, nameArg.ToString());
                        return false;
                    }
                    else
                    {
                        // Legacy behavior: it's ok for the column name not to be a constant string - no validation will be performed.
                    }
                }
            }

            var columnType = DType.Invalid;

            if (columnName.IsValid)
            {
                // Verify that the name exists.
                if (!sourceType.TryGetType(columnName, out columnType))
                {
                    sourceType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, nameArg);
                    return false;
                }
                else if (!columnType.IsPrimitive)
                {
                    errors.EnsureError(nameArg, TexlStrings.ErrSortWrongType);
                    return false;
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
            if (nameArg != null && columnType.IsValid && !columnType.Accepts(column.Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                errors.EnsureError(
                    DocumentErrorSeverity.Severe,
                    valuesArg,
                    TexlStrings.ErrTypeError_Arg_Expected_Found,
                    columnName.Value,
                    columnType.GetKindString(),
                    column.Type.GetKindString());
                return false;
            }

            return true;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex >= 0;
        }

        private bool IsColumnSortable(TexlNode node, DName nodeName, TexlBinding binding, SortOpMetadata sortMetadata)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(nodeName.IsValid);
            Contracts.AssertValue(nodeName.Value);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(sortMetadata);

            var columnPath = DPath.Root.Append(nodeName);
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

            if (binding.ErrorContainer.HasErrors(node))
            {
                return false;
            }

            DName columnName;
            if (binding.Features.SupportColumnNamesAsIdentifiers)
            {
                if (node is not FirstNameNode identifierNode)
                {
                    return false;
                }

                columnName = identifierNode.Ident.Name;
            }
            else
            {
                if (node.Kind != NodeKind.StrLit)
                {
                    return false;
                }

                columnName = new DName(node.AsStrLit().VerifyValue().Value);
            }
            
            return IsColumnSortable(node, columnName, binding, metadata);
        }

        private bool IsValidSortOrderNode(TexlNode node, SortOpMetadata metadata, TexlBinding binding, DPath columnPath)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(metadata);
            Contracts.AssertValue(binding);
            Contracts.AssertValid(columnPath);

            if (binding.IsAsync(node))
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Function:{0}, SortOrderNode is async", Name);
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

            if (callNode.Args.Count == 3)
            {
                // Check if using the SortByColumns(source, column, ordertable) overload, which is not delegable
                var secondArgType = binding.GetType(callNode.Args.ChildNodes[2]);
                if (secondArgType.IsTable)
                {
                    return false;
                }
            }

            SortOpMetadata metadata = null;
            if (TryGetEntityMetadata(callNode, binding, out IDelegationMetadata delegationMetadata))
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
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

                DName columnName;
                if (binding.Features.SupportColumnNamesAsIdentifiers)
                {
                    columnName = args[i].AsFirstName().VerifyValue().Ident.Name;
                }
                else
                {
                    columnName = new DName(args[i].AsStrLit().VerifyValue().Value);
                }

                var sortOrderNode = (i + 1) < cargs ? args[i + 1] : null;
                var sortOrder = sortOrderNode == null ? defaultSortOrder : string.Empty;
                if (sortOrderNode != null)
                {
                    if (!IsValidSortOrderNode(sortOrderNode, metadata, binding, DPath.Root.Append(columnName)))
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

            order = order.ToLowerInvariant();

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

            DName columnName;
            DType columnType;
            var retVal = false;

            if (callNode.Args.Count == 3 && binding.GetType(callNode.Args.ChildNodes[2]).IsTableNonObjNull)
            {
                // Using the SortByColumns(source, column, ordertable) overload
                if (binding.Features.SupportColumnNamesAsIdentifiers)
                {
                    var firstName = args[1].AsFirstName();
                    if (firstName == null)
                    {
                        return false;
                    }

                    columnName = firstName.Ident.Name;
                }
                else
                {
                    var strLitNode = args[1].AsStrLit();
                    if (strLitNode == null)
                    {
                        return false;
                    }

                    columnName = new DName(strLitNode.Value);
                }

                Contracts.Assert(dsType.Contains(new DName(columnName)));

                if (!dsType.TryGetType(columnName, out columnType))
                {
                    return false;
                }

                return dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            for (var i = 1; i < args.Count; i += 2)
            {
                if (binding.Features.SupportColumnNamesAsIdentifiers)
                {
                    var firstName = args[i].AsFirstName();
                    if (firstName == null)
                    {
                        continue;
                    }

                    columnName = firstName.Ident.Name;
                }
                else
                {
                    var strLitNode = args[i].AsStrLit();
                    if (strLitNode == null)
                    {
                        continue;
                    }

                    columnName = new DName(strLitNode.Value);
                }

                Contracts.Assert(dsType.Contains(columnName));
                if (!dsType.TryGetType(columnName, out columnType))
                {
                    continue;
                }

                retVal |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retVal;
        }

        public override bool IsIdentifierParam(int index)
        {
            return (index % 2) == 1;
        }
    }
}
