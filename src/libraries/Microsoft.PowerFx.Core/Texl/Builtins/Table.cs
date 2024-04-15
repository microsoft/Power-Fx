// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Table(rec/table, rec/table, ...)
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
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Ensure that all args (if any) are records with compatible schemas.
            var resultType = DType.EmptyRecord;
            for (var i = 0; i < argTypes.Length; i++)
            {
                var argType = argTypes[i];
                var argTypeRecord = argType.IsTableNonObjNull ? argType.ToRecord() : argType;
                var isChildTypeAllowedInTable = !argType.IsDeferred && !argType.IsVoid;

                if (!argTypeRecord.IsRecord)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedRecordOrTable);
                    isValid = false;
                }
                else if (!isChildTypeAllowedInTable)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTableDoesNotAcceptThisType);
                    return false;
                }
                else
                {
                    if (DType.TryUnionWithCoerce(
                        resultType,
                        argTypeRecord,
                        context.Features,
                        coerceToLeftTypeOnly: context.Features.StronglyTypedBuiltinEnums || context.Features.PowerFxV1CompatibilityRules,
                        out var newType,
                        out bool coercionNeeded))
                    {
                        resultType = newType;

                        if (coercionNeeded)
                        {
                            var coerceType = argType.IsTable ? resultType.ToTable() : resultType;
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coerceType);
                        }
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTableDoesNotAcceptThisType);
                        isValid = false;
                    }
                }

                Contracts.Assert(resultType.IsRecord);
            }

            returnType = resultType.ToTable();

            return isValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);

            for (var i = 0; i < argTypes.Length; i++)
            {
                // show warning when the node is pageable as data would be truncated at this point
                if (argTypes[i].IsTableNonObjNull && binding.IsPageable(args[i]))
                {
                    errors.EnsureError(DocumentErrorSeverity.Warning, args[i], TexlStrings.ErrTruncatedArgWarning, args[i].ToString(), Name);
                    continue;
                }
            }
        }
    }

    internal class TableFunction_UO : BuiltinFunction
    {
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
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var isValid = base.CheckTypes(context, args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.UntypedObject, ColumnName_Value));
            returnType = rowType.ToTable();

            return isValid;
        }
    }
}
