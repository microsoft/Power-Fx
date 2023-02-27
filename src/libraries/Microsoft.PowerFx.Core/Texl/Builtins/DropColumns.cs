﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // DropColumns(source:*[...], name:s, name:s, ...)
    internal sealed class DropColumnsFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool HasColumnIdentifiers => true;

        public override bool SupportsParamCoercion => false;

        public DropColumnsFunction()
            : base("DropColumns", TexlStrings.AboutDropColumns, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DropColumnsArg1, TexlStrings.DropColumnsArg2 };
            yield return new[] { TexlStrings.DropColumnsArg1, TexlStrings.DropColumnsArg2, TexlStrings.DropColumnsArg2 };
            yield return new[] { TexlStrings.DropColumnsArg1, TexlStrings.DropColumnsArg2, TexlStrings.DropColumnsArg2, TexlStrings.DropColumnsArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.DropColumnsArg1, TexlStrings.DropColumnsArg2);
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

            var supportColumnNamesAsIdentifiers = context.Features.HasFlag(Features.SupportColumnNamesAsIdentifiers);

            // The result type has N fewer columns, as specified by (args[1],args[2],args[3],...)
            var count = args.Length;
            for (var i = 1; i < count; i++)
            {
                string expectedColumnName = null;
                var nameArg = args[i];
                var nameArgType = argTypes[i];

                // Verify we have a string literal for the column name. Accd to spec, we don't support
                // arbitrary expressions that evaluate to string values, because these values contribute to
                // type analysis, so they need to be known upfront (before DropColumns executes).                            
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

                    // Argument '{0}' is not a valid identifier.
                    errors.EnsureError(DocumentErrorSeverity.Severe, nameArg, TexlStrings.ErrArgNotAValidIdentifier_Name, expectedColumnName);
                    continue;
                }

                var columnName = new DName(DType.TryGetLogicalNameForColumn(argTypes[0], expectedColumnName, out var logicalName)
                                         ? logicalName
                                         : expectedColumnName);

                // Verify that the name exists.
                if (!returnType.TryGetType(columnName, out var columnType))
                {
                    fArgsValid = false;
                    returnType.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, nameArg);
                    continue;
                }

                // Drop the specified column from the result type.
                var fError = false;
                returnType = returnType.Drop(ref fError, DPath.Root, columnName);
                Contracts.Assert(!fError);
            }

            return fArgsValid;
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
