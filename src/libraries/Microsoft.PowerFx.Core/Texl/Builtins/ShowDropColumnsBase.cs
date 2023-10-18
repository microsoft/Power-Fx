// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // DropColumns(source:*[...], name:s, name:s, ...)
    // DropColumns(source:![...], name:s, name:s, ...)
    // ShowColumns(source:*[...], name:s, name:s, ...)
    // ShowColumns(source:![...], name:s, name:s, ...)
    internal class ShowDropColumnsFunctionBase : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool HasColumnIdentifiers => true;

        public override bool SupportsParamCoercion => false;

        public override bool RecordFirstArgumentCanCreateScope => true;

        private readonly bool _isShowColumns;

        public ShowDropColumnsFunctionBase(bool isShowColumns)
            : base(
                  name: isShowColumns ? "ShowColumns" : "DropColumns",
                  description: isShowColumns ? TexlStrings.AboutShowColumns : TexlStrings.AboutDropColumns,
                  fc: FunctionCategories.Table,
                  returnType: DType.EmptyTable,
                  maskLambdas: 0,
                  arityMin: 2,
                  arityMax: int.MaxValue,
                  DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
            _isShowColumns = isShowColumns;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            var arg1 = _isShowColumns ? TexlStrings.ShowColumnsArg1 : TexlStrings.DropColumnsArg1;
            var arg2 = _isShowColumns ? TexlStrings.ShowColumnsArg2 : TexlStrings.DropColumnsArg2;
            yield return new[] { arg1, arg2 };
            yield return new[] { arg1, arg2, arg2 };
            yield return new[] { arg1, arg2, arg2, arg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                var arg1 = _isShowColumns ? TexlStrings.ShowColumnsArg1 : TexlStrings.DropColumnsArg1;
                var arg2 = _isShowColumns ? TexlStrings.ShowColumnsArg2 : TexlStrings.DropColumnsArg2;
                return GetGenericSignatures(arity, arg1, arg2);
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

            returnType = ReturnType;
            nodeToCoercedTypeMap = null;

            var isRecord = false;
            var fArgsValid = true;
            if (argTypes[0].IsTable)
            {
                returnType = argTypes[0];
            }
            else if (argTypes[0].IsRecord)
            {
                returnType = argTypes[0];
                isRecord = true;
            }
            else
            {
                fArgsValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedTable_Func, Name);
            }

            Contracts.Assert(returnType.IsTable || returnType.IsRecord);

            if (fArgsValid)
            {
                fArgsValid = base.CheckType(context, args[0], argTypes[0], isRecord ? DType.EmptyRecord : ParamTypes[0], errors, ref nodeToCoercedTypeMap);
            }

            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;
            var colsToKeep =
                _isShowColumns
                ? (isRecord ? DType.EmptyRecord : DType.EmptyTable)
                : returnType;

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

                var columnName = _isShowColumns ?
                    new DName(expectedColumnName) :
                    new DName(
                        DType.TryGetLogicalNameForColumn(argTypes[0], expectedColumnName, out var logicalName)
                        ? logicalName
                        : expectedColumnName);

                // Verify that the name exists.
                if (_isShowColumns)
                {
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
                else
                {
                    if (!colsToKeep.TryGetType(columnName, out var columnType))
                    {
                        fArgsValid = false;
                        colsToKeep.ReportNonExistingName(FieldNameKind.Logical, errors, columnName, nameArg);
                        continue;
                    }

                    // Drop the specified column from the result type.
                    var fError = false;
                    colsToKeep = colsToKeep.Drop(ref fError, DPath.Root, columnName);
                    Contracts.Assert(!fError);
                }
            }

            returnType = colsToKeep;

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
