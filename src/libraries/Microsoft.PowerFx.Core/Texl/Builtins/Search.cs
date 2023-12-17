// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class SearchFunction : FunctionWithTableInput
    {
        // Return true if this function affects datasource query options.
        public override bool AffectsDataSourceQueryOptions => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasColumnIdentifiers => true;

        public SearchFunction()
            : base("Search", TexlStrings.AboutSearch, FunctionCategories.Table, DType.EmptyTable, 0, 3, int.MaxValue, DType.EmptyTable, DType.String, DType.String)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(Features features, int index)
        {
            if (!features.SupportColumnNamesAsIdentifiers)
            {
                return ParamIdentifierStatus.NeverIdentifier;
            }

            return index > 1 ? ParamIdentifierStatus.AlwaysIdentifier : ParamIdentifierStatus.NeverIdentifier;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SearchArg1, TexlStrings.SearchArg2, TexlStrings.SearchArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > MinArity)
            {
                return GetOverloads(arity);
            }

            return base.GetSignatures(arity);
        }

        private IEnumerable<TexlStrings.StringGetter[]> GetOverloads(int arity)
        {
            Contracts.Assert(MinArity < arity);

            const int OverloadCount = 1;
            var overloads = new List<TexlStrings.StringGetter[]>(OverloadCount);

            // Limit the signature length of params descriptions.
            int argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength : arity;
            var overload = new TexlStrings.StringGetter[argCount];
            overload[0] = TexlStrings.SearchArg1;
            overload[1] = TexlStrings.SearchArg2;
            for (int iarg = 2; iarg < argCount; iarg++)
            {
                overload[iarg] = TexlStrings.SearchArg3;
            }

            overloads.Add(overload);
            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(overloads);
        }

        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex >= 0;
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
            if (!fValid)
            {
                return fValid;
            }

            Contracts.Assert(returnType.IsTable);

            var argLen = args.Length;
            returnType = argTypes[0];
            DType sourceType = argTypes[0];

            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;
            if (argTypes[1].Kind != DKind.String)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                fValid = false;
            }

            // Check if table contains any searchable columns (column type is string).
            if (!sourceType.GetAllNames(DPath.Root).Any(name => name.Type.Kind == DKind.String))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrSearchWrongTableType);
                fValid = false;
            }

            for (var i = 2; i < argLen; i++)
            {
                var nameArg = args[i];
                if (!base.TryGetColumnLogicalName(argTypes[0], supportColumnNamesAsIdentifiers, nameArg, errors, out var columnName, out var columnType))
                {
                    fValid = false;
                    continue;
                }
                else if (!IsValidSearchableColumnType(columnType))
                {
                    fValid = false;
                    errors.EnsureError(args[i], TexlStrings.ErrSearchWrongType);
                }
            }

            return fValid;
        }

        internal static bool IsValidSearchableColumnType(DType type)
        {
            Contracts.AssertValid(type);

            return type.Kind == DKind.String;
        }

        internal static bool IsColumnSearchable(DPath columnPath, IExternalDataSource dataSource)
        {
            Contracts.AssertValid(columnPath);
            Contracts.AssertValue(dataSource);

            var metadata = dataSource.DelegationMetadata?.FilterDelegationMetadata;
            if (metadata == null)
            {
                return false;
            }

            return metadata.IsColumnSearchable(columnPath);
        }

        private bool IsValidSearchableColumnNode(TexlNode node, TexlBinding binding, IExternalDataSource dataSource)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(dataSource);

            if (binding.ErrorContainer.HasErrors(node))
            {
                return false;
            }

            var columnsAsIdentifiers = binding.Features.SupportColumnNamesAsIdentifiers;
            DName columnName = default;
            if (!columnsAsIdentifiers && node.Kind == NodeKind.StrLit)
            {
                columnName = new DName(node.AsStrLit().Value);
            }
            else if (columnsAsIdentifiers && node.Kind == NodeKind.FirstName)
            {
                columnName = node.AsFirstName().Ident.Name;
                var dsNode = node.Parent.AsList()?.Children[0];
                if (dsNode != null)
                {
                    var dsType = binding.GetType(dsNode);
                    if (DType.TryGetLogicalNameForColumn(dsType, columnName.Value, out var logicalName))
                    {
                        columnName = new DName(logicalName);
                    }
                }
            }

            if (!columnName.IsValid)
            {
                return false;
            }

            if (!IsColumnSearchable(DPath.Root.Append(columnName), dataSource))
            {
                SuggestDelegationHint(node, binding);
                return false;
            }

            return true;
        }

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            // Ignore delegation warning
            if (!CheckArgsCount(callNode, binding, DocumentErrorSeverity.Moderate))
            {
                return false;
            }

            var args = Contracts.VerifyValue(callNode.Args.Children);
            DType dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            int cargs = args.Count;
            bool retval = false;

            for (int i = 2; i < cargs; i++)
            {
                base.TryGetColumnLogicalName(dsType, binding.Features.SupportColumnNamesAsIdentifiers, args[i], DefaultErrorContainer, out DName columnName, out DType columnType).Verify();
                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            if (!TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.Filter, out var dataSource))
            {
                return false;
            }

            var args = Contracts.VerifyValue(callNode.Args.Children);
            int cargs = args.Count;

            FilterOpMetadata metadata = Contracts.VerifyValue(dataSource.DelegationMetadata.FilterDelegationMetadata);
            for (int i = 2; i < cargs; i++)
            {
                if (!IsValidSearchableColumnNode(args[i], binding, dataSource))
                {
                    var message = "ColumnSearchable: false";
                    AddSuggestionMessageToTelemetry(message, args[i], binding);
                    return false;
                }
            }

            return true;
        }

        public override ArgPreprocessor GetArgPreprocessor(int index, int argCount)
        {
            return index == 1 ? ArgPreprocessor.ReplaceBlankWithEmptyString : base.GetArgPreprocessor(index, argCount);
        }
    }
}
