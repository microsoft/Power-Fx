﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ShowColumns(source:*[...], name:s, name:s, ...)
    internal sealed class ShowColumnsFunction : FunctionWithTableInput
    {
        public override bool AffectsDataSourceQueryOptions => true;

        public override bool IsSelfContained => true;

        public override bool HasColumnIdentifiers => true;

        public override bool SupportsParamCoercion => false;

        public ShowColumnsFunction()
            : base("ShowColumns", TexlStrings.AboutShowColumns, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ShowColumnsArg1, TexlStrings.ShowColumnsArg2 };
            yield return new[] { TexlStrings.ShowColumnsArg1, TexlStrings.ShowColumnsArg2, TexlStrings.ShowColumnsArg2 };
            yield return new[] { TexlStrings.ShowColumnsArg1, TexlStrings.ShowColumnsArg2, TexlStrings.ShowColumnsArg2, TexlStrings.ShowColumnsArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.ShowColumnsArg1, TexlStrings.ShowColumnsArg2);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fArgsValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            if (!argTypes[0].IsTable)
            {
                fArgsValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedTable_Func, Name);
            }
            else
            {
                returnType = argTypes[0];
            }

            var colsToKeep = DType.EmptyTable;
            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;

            // The result type has N columns, as specified by (args[1],args[2],args[3],...)
            var count = args.Length;
            for (var i = 1; i < count; i++)
            {
                var nameArg = args[i];
                var nameArgType = argTypes[i];

                string expectedColumnName = null;

                if (supportColumnNamesAsIdentifiers)
                {
                    if (nameArg is not FirstNameNode identifierNode)
                    {
                        fArgsValid = false;

                        // Argument '{0}' is invalid, expected an identifier.
                        errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrExpectedIdentifierArg_Name, nameArg.ToString());
                        continue;
                    }

                    expectedColumnName = identifierNode.Ident.Name;
                }
                else
                {
                    StrLitNode strLitNode = nameArg.AsStrLit();

                    if (nameArgType.Kind != DKind.String || strLitNode == null)
                    {
                        fArgsValid = false;

                        // Argument '{0}' is invalid, expected a text literal.
                        errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrExpectedStringLiteralArg_Name, nameArg.ToString());
                        continue;
                    }

                    expectedColumnName = strLitNode.Value;
                }

                // Verify that the name is valid.
                if (!DName.IsValidDName(expectedColumnName))
                {
                    fArgsValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrArgNotAValidIdentifier_Name, expectedColumnName);
                    continue;
                }

                var columnName = new DName(expectedColumnName);

                // Verify that the name exists.
                if (!returnType.TryGetType(columnName, out var columnType))
                {
                    fArgsValid = false;
                    returnType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, args[i]);
                    continue;
                }

                // Verify that the name was only specified once.
                if (colsToKeep.TryGetType(columnName, out var existingColumnType))
                {
                    fArgsValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Warning, nameArg, TexlStrings.WarnColumnNameSpecifiedMultipleTimes_Name, columnName);
                    continue;
                }

                // Make a note of which columns are being kept.
                Contracts.Assert(columnType.IsValid);
                colsToKeep = colsToKeep.Add(columnName, columnType);
            }

            // Drop everything but the columns that need to be kept.
            returnType = colsToKeep;

            return fArgsValid;
        }

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();

            var dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            var resultType = binding.GetType(callNode).VerifyValue();

            var retval = false;
            foreach (var typedName in resultType.GetNames(DPath.Root))
            {
                var columnType = typedName.Type;
                var columnName = typedName.Name.Value;

                Contracts.Assert(dsType.Contains(new DName(columnName)));

                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex >= 0;
        }

        public override bool IsIdentifierParam(int index)
        {
            return index > 0;
        }
    }
}
