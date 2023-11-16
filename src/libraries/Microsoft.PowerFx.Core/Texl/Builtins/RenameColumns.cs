// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    // RenameColumns(source:*[...], oldName:s, newName:s)
    // RenameColumns(source:![...], oldName:s, newName:s)
    internal sealed class RenameColumnsFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasColumnIdentifiers => true;

        public RenameColumnsFunction()
            : base("RenameColumns", TexlStrings.AboutRenameColumns, FunctionCategories.Table, DType.EmptyTable, 0, 3, int.MaxValue, DType.EmptyTable)
        {
            // RenameColumns(source, oldName, newName, oldName, newName, ..., oldName, newName, ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 5, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 9);
            ScopeInfo = new FunctionScopeInfo(this, canBeCreatedByRecord: true);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.RenameColumnsArg1, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3 };
            yield return new[] { TexlStrings.RenameColumnsArg1, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3 };
            yield return new[] { TexlStrings.RenameColumnsArg1, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3, TexlStrings.RenameColumnsArg2, TexlStrings.RenameColumnsArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetOverloadsRenameColumns(arity);
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
            var isRecord = false;
            nodeToCoercedTypeMap = null;
            bool isValidInvocation;
            if (argTypes[0].IsTable)
            {
                isValidInvocation = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
                returnType = argTypes[0];
            }
            else if (argTypes[0].IsRecord)
            {
                returnType = argTypes[0];
                isRecord = true;
                isValidInvocation = base.CheckType(context, args[0], argTypes[0], DType.EmptyRecord, errors, ref nodeToCoercedTypeMap);
            }
            else
            {
                isValidInvocation = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedTable_Func, Name);
            }

            Contracts.Assert(returnType.IsTable || returnType.IsRecord);

            // If the first argument has a meta field, we will need to expand the metafield to get the actual
            // return type.
            if (returnType.TryGetType(new DName(DType.MetaFieldName), out var metaFieldType))
            {
                bool isError = false;
                returnType = returnType.Drop(ref isError, DPath.Root, new DName(DType.MetaFieldName));
                Contracts.Assert(!isError);
                returnType = DType.Union(returnType, isRecord ? metaFieldType.ToRecord() : metaFieldType.ToTable(), useLegacyDateTimeAccepts: false, features: context.Features);
            }

            int count = args.Length;
            if ((count & 1) == 0)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0].Parent.CastList().Parent.CastCall(), TexlStrings.ErrBadArityOdd, count);
            }

            var columnReplacements = new List<ColumnReplacement>();
            var newColumnNames = new HashSet<string>();
            var oldColumnNames = new HashSet<string>();

            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;

            for (var i = 1; i < count - 1; i += 2)
            {
                TexlNode oldNameArg = args[i], newNameArg = args[i + 1];
                DType oldNameArgType = argTypes[i], newNameArgType = argTypes[i + 1];

                if (!base.TryGetColumnLogicalName(argTypes[0], supportColumnNamesAsIdentifiers, oldNameArg, errors, out DName oldColumnName, out var oldColumnType))
                {
                    return false;
                }

                if (!base.TryGetColumnLogicalName(null, supportColumnNamesAsIdentifiers, newNameArg, errors, out DName newColumnName))
                {
                    return false;
                }

                var oldColumnStringName = oldColumnName.Value;
                var newColumnStringName = newColumnName.Value;

                if (oldColumnNames.Contains(oldColumnStringName))
                {
                    isValidInvocation = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, oldNameArg, TexlStrings.ErrColRenamedTwice_Name, oldColumnName);
                    return false;
                }

                // Verify that the new names doesn't collide with existing columns.
                if (newColumnName != oldColumnName && (
                        returnType.TryGetType(newColumnName, out var existingColumnType) ||
                        DType.TryGetLogicalNameForColumn(returnType, newColumnName, out var _) ||
                        newColumnNames.Contains(newColumnStringName)))
                {
                    if (DType.TryGetDisplayNameForColumn(returnType, newColumnName, out var colName))
                    {
                        newColumnName = new DName(colName);
                    }

                    isValidInvocation = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, newNameArg, TexlStrings.ErrColExists_Name, newColumnName);
                    return false;
                }

                if (oldColumnName != newColumnName)
                {
                    columnReplacements.Add(ColumnReplacement.Create(oldColumnName, newColumnName, oldColumnType));
                    oldColumnNames.Add(oldColumnName);
                    newColumnNames.Add(newColumnName);
                }
            }

            foreach (var replacement in columnReplacements)
            {
                // Replace all columns
                bool isError = false;
                returnType = returnType
                    .Drop(ref isError, DPath.Root, replacement.OldColumnName)
                    .Add(ref isError, DPath.Root, replacement.NewColumnName, replacement.ColumnType);
                Contracts.Assert(!isError);
            }

            return isValidInvocation;
        }

        // Gets the overloads for the RenameColumns function for the specified arity.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsRenameColumns(int arity)
        {
            Contracts.Assert(arity >= 4);

            const int OverloadCount = 2;

            var overloads = new List<TexlStrings.StringGetter[]>(OverloadCount);

            // Limit the argCount avoiding potential OOM
            int argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength : arity;
            for (int ioverload = 0; ioverload < OverloadCount; ioverload++)
            {
                int iArgCount = (argCount | 1) + (ioverload * 2);
                var overload = new TexlStrings.StringGetter[iArgCount];
                overload[0] = TexlStrings.RenameColumnsArg1;
                for (int iarg = 1; iarg < iArgCount; iarg += 2)
                {
                    overload[iarg] = TexlStrings.RenameColumnsArg2;
                    overload[iarg + 1] = TexlStrings.RenameColumnsArg3;
                }

                overloads.Add(overload);
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(overloads);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0 || (argumentIndex & 0x1) == 1;
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(int index)
        {
            Contracts.Assert(index >= 0);

            return index > 0 ? ParamIdentifierStatus.AlwaysIdentifier : ParamIdentifierStatus.NeverIdentifier;
        }

        public override bool AffectsDataSourceQueryOptions => true;

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

            bool retval = false;

            for (var i = 1; i < args.Count - 1; i += 2)
            {
                string columnName;
                DType columnType;
                if (binding.Features.SupportColumnNamesAsIdentifiers)
                {
                    FirstNameNode columnNode = args[i].AsFirstName();

                    // This is a bug in the existing implementation - with column names as
                    // string this will always return a string, although it seems like it
                    // intended to return the type of the column being renamed. Since this code
                    // will be obsoleted soon, just keep the existing logic for now.
                    columnType = DType.String;

                    if (columnType.Kind != DKind.String || columnNode == null)
                    {
                        continue;
                    }

                    columnName = columnNode.Ident.Name.Value;
                }
                else
                {
                    columnType = binding.GetType(args[i]);
                    StrLitNode columnNode = args[i].AsStrLit();
                    if (columnType.Kind != DKind.String || columnNode == null)
                    {
                        continue;
                    }

                    columnName = columnNode.Value;
                }

                Contracts.Assert(dsType.Contains(new DName(columnName)));

                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }

        private class ColumnReplacement
        {
            public DName OldColumnName { get; private set; }

            public DName NewColumnName { get; private set; }

            public DType ColumnType { get; private set; }

            public static ColumnReplacement Create(DName oldName, DName newName, DType type)
            {
                return new ColumnReplacement
                {
                    OldColumnName = oldName,
                    NewColumnName = newName,
                    ColumnType = type
                };
            }
        }
    }
}
