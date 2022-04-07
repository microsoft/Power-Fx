// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Table(rec, rec, ...)
    internal class TableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public TableFunction()
            : base("Table", TexlStrings.AboutTable, FunctionCategories.Table, DType.EmptyTable, 0, 0, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TableArg1 };
            yield return new[] { TexlStrings.TableArg1, TexlStrings.TableArg1 };
            yield return new[] { TexlStrings.TableArg1, TexlStrings.TableArg1, TexlStrings.TableArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.TableArg1);
            }

            return base.GetSignatures(arity);
        }

        // Typecheck an invocation of Table.
        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var isValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Ensure that all args (if any) are records with compatible schemas.
            var rowType = DType.EmptyRecord;
            for (var i = 0; i < argTypes.Length; i++)
            {
                var argType = argTypes[i];
                if (!argType.IsRecord)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedRecord);
                    isValid = false;
                }
                else if (!rowType.CanUnionWith(argType))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrIncompatibleRecord);
                    isValid = false;
                }
                else
                {
                    var isUnionError = false;
                    rowType = DType.Union(ref isUnionError, rowType, argType);
                    Contracts.Assert(!isUnionError);
                    Contracts.Assert(rowType.IsRecord);
                }
            }

            Contracts.Assert(rowType.IsRecord);
            returnType = rowType.ToTable();

            return isValid;
        }
    }

    internal class TableFunction_UO : BuiltinFunction
    {
        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public TableFunction_UO()
            : base("Table", TexlStrings.AboutTable, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TableArg1 };
        }

        // Typecheck an invocation of Table.
        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var isValid = CheckInvocation(args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.UntypedObject, ColumnName_Value));
            returnType = rowType.ToTable();

            return isValid;
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
