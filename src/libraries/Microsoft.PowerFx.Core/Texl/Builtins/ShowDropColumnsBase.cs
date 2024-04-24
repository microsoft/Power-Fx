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
            ScopeInfo = new FunctionScopeInfo(this, canBeCreatedByRecord: true);
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
            Dictionary<DName, DName> newDisplayNameMapping = null;

            // With Power Fx 1.0, we will propagate the display names to the new result type.
            // We will not change the pre-v1 logic as it may break some corner scenarios.
            if (_isShowColumns && context.Features.PowerFxV1CompatibilityRules && returnType.DisplayNameProvider != null)
            {
                newDisplayNameMapping = new Dictionary<DName, DName>();
            }

            var count = args.Length;
            for (var i = 1; i < count; i++)
            {
                var nameArg = args[i];

                if (!base.TryGetColumnLogicalName(argTypes[0], supportColumnNamesAsIdentifiers, nameArg, errors, out var columnName))
                {
                    fArgsValid = false;
                    continue;
                }

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
                    if (newDisplayNameMapping != null && returnType.DisplayNameProvider.TryGetDisplayName(columnName, out var displayName))
                    {
                        newDisplayNameMapping.Add(columnName, displayName);
                    }
                }
                else
                {
                    // Verify that the name exists.
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
            if (newDisplayNameMapping != null)
            {
                var newDisplayNameProvider = DisplayNameProvider.New(newDisplayNameMapping);
                returnType = DType.AttachOrDisableDisplayNameProvider(returnType, newDisplayNameProvider);
            }

            return fArgsValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex >= 0;
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(Features features, int index)
        {
            if (!features.SupportColumnNamesAsIdentifiers)
            {
                return ParamIdentifierStatus.NeverIdentifier;
            }

            return index > 0 ? ParamIdentifierStatus.AlwaysIdentifier : ParamIdentifierStatus.NeverIdentifier;
        }
    }
}
