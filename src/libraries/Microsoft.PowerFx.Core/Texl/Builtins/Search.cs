﻿// Copyright (c) Microsoft Corporation.
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
using Microsoft.PowerFx.Core.Texl.Builtins;
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

        public SearchFunction()
            : base("Search", TexlStrings.AboutSearch, FunctionCategories.Table, DType.EmptyTable, 0, 3, int.MaxValue, DType.EmptyTable, DType.String, DType.String)
        { 
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
                TexlNode colNameArg = args[i];
                DType colNameArgType = argTypes[i];
                StrLitNode nameNode;

                if (colNameArgType.Kind != DKind.String || (nameNode = colNameArg.AsStrLit()) == null)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, colNameArg, TexlStrings.ErrStringExpected);
                    fValid = false;
                }
                else if (DName.IsValidDName(nameNode.Value))
                {
                    // Verify that the name is valid.
                    DName columnName = new DName(nameNode.Value);

                    // Verify that the name exists.
                    if (!sourceType.TryGetType(columnName, out DType columnType))
                    {
                        sourceType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, args[i]);
                        fValid = false;
                    }
                    else if (!IsValidSearchableColumnType(columnType))
                    {
                        fValid = false;
                        errors.EnsureError(colNameArg, TexlStrings.ErrSearchWrongType);
                    }
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameNode, TexlStrings.ErrArgNotAValidIdentifier_Name, nameNode.Value);
                    fValid = false;
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

            if (binding.ErrorContainer.HasErrors(node) || node.Kind != NodeKind.StrLit)
            {
                return false;
            }

            StrLitNode columnNode = Contracts.VerifyValue(node.AsStrLit());
            string columnName = columnNode.Value;
            if (string.IsNullOrEmpty(columnName) || !IsColumnSearchable(DPath.Root.Append(new DName(columnName)), dataSource))
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
                DType columnType = binding.GetType(args[i]);
                StrLitNode columnNode = args[i].AsStrLit();
                if (columnType.Kind != DKind.String || columnNode == null)
                {
                    continue;
                }

                string columnName = columnNode.Value;

                Contracts.Assert(dsType.Contains(new DName(columnName)));

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
#pragma warning disable CA1305 // Specify IFormatProvider
                    var message = string.Format("ColumnSearchable: false");
#pragma warning restore CA1305 // Specify IFormatProvider
                    AddSuggestionMessageToTelemetry(message, args[i], binding);
                    return false;
                }
            }

            return true;
        }
    }
}
