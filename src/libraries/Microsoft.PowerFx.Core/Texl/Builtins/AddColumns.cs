// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    // AddColumns(source:*[...], name:s, valueFunc:func<_>, name:s, valueFunc:func<_>, ...)
    // Corresponding DAX function: AddColumns
    internal sealed class AddColumnsFunction : FunctionWithTableInput
    {
        public override bool SkipScopeForInlineRecords => true;

        public override bool HasLambdas => true;

        public override bool HasIdentifiers => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public AddColumnsFunction()
            : base("AddColumns", TexlStrings.AboutAddColumns, FunctionCategories.Table, DType.EmptyTable, 0, 3, int.MaxValue, DType.EmptyTable)
        {
            // AddColumns(source, name, valueFunc, name, valueFunc, ..., name, valueFunc, ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 5, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 9);
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.AddColumnsArg1, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3 };
            yield return new[] { TexlStrings.AddColumnsArg1, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3 };
            yield return new[] { TexlStrings.AddColumnsArg1, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3, TexlStrings.AddColumnsArg2, TexlStrings.AddColumnsArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetOverloadsAddColumns(arity);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fArgsValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // The first arg determines the scope type for the lambda params, and the return type.
            fArgsValid &= ScopeInfo.CheckInput(args[0], argTypes[0], errors, out var typeScope);
            Contracts.Assert(typeScope.IsRecord);

            // The result type has N additional columns, as specified by (args[1],args[2]), (args[3],args[4]), ... etc.
            returnType = typeScope.ToTable();

            var count = args.Length;
            if ((count & 1) == 0)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0].Parent.CastList().Parent.CastCall(), TexlStrings.ErrBadArityOdd, count);
            }

            var supportIndentifiers = binding.Features.HasFlag(Features.SupportIdentifiers);

            for (var i = 1; i < count; i += 2)
            {
                string expectedColumnName = null;
                var nameArg = args[i];
                var nameArgType = argTypes[i];

                // Verify we have a string literal for the column name. Accd to spec, we don't support
                // arbitrary expressions that evaluate to string values, because these values contribute to
                // type analysis, so they need to be known upfront (before AddColumns executes).
                if (supportIndentifiers)
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

                    if (nameArgType.Kind != DKind.String && strLitNode == null)
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

                var columnName = new DName(expectedColumnName);
                if (DType.TryGetDisplayNameForColumn(typeScope, columnName, out var colName))
                {
                    columnName = new DName(colName);
                }

                // Verify that the name doesn't already exist as either a logical or display name
                if (typeScope.TryGetType(columnName, out var columnType) || DType.TryGetLogicalNameForColumn(typeScope, columnName, out _))
                {
                    fArgsValid = false;

                    // A column named '{0}' already exists.
                    errors.EnsureError(DocumentErrorSeverity.Moderate, nameArg, TexlStrings.ErrColExists_Name, columnName);
                    continue;
                }

                if (i + 1 >= count)
                {
                    break;
                }

                columnType = argTypes[i + 1];

                // Augment the result type to include the specified column, and verify that it
                // hasn't been specified already within the same invocation.
                var fError = false;
                returnType = returnType.Add(ref fError, DPath.Root, columnName, columnType);
                if (fError)
                {
                    fArgsValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Moderate, nameArg, TexlStrings.ErrColConflict_Name, columnName);
                    continue;
                }
            }

            return fArgsValid;
        }

        // Gets the overloads for the AddColumns function for the specified arity.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsAddColumns(int arity)
        {
            Contracts.Assert(arity >= 4);

            const int OverloadCount = 2;

            // REVIEW ragru: cache these and enumerate from the cache...

            var overloads = new List<TexlStrings.StringGetter[]>(OverloadCount);

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength : arity;
            for (var ioverload = 0; ioverload < OverloadCount; ioverload++)
            {
                var iArgCount = (argCount | 1) + (ioverload * 2);
                var overload = new TexlStrings.StringGetter[iArgCount];
                overload[0] = TexlStrings.AddColumnsArg1;
                for (var iarg = 1; iarg < iArgCount; iarg += 2)
                {
                    overload[iarg] = TexlStrings.AddColumnsArg2;
                    overload[iarg + 1] = TexlStrings.AddColumnsArg3;
                }

                overloads.Add(overload);
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(overloads);
        }

        public override bool IsLambdaParam(int index)
        {
            Contracts.Assert(index >= 0);

            // Left to right mask (infinite): ...101010100 == 0x...555554
            return index >= 2 && ((index & 1) == 0);
        }

        public override bool IsIdentifierParam(int index)
        {
            Contracts.Assert(index >= 0);

            // Left to right mask (infinite): ...010101010 
            return index >= 1 && ((index & 1) == 1);
        }

        public override bool AllowsRowScopedParamDelegationExempted(int index)
        {
            Contracts.Assert(index >= 0);

            return IsLambdaParam(index);
        }
    }
}
