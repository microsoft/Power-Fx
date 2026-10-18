// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal class UpdateFunction : RemoveFunctionBase, IFunctionInvoker
    {
        public override bool SupportsParamCoercion => true;

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Update;

        public UpdateFunction()
            : base("Update", AboutUpdate, FunctionCategories.Table | FunctionCategories.Behavior, DType.Unknown, 0, 3, 4, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { UpdateDataSourceArg, UpdateOldRecordArg, UpdateNewRecordArg };
            yield return new[] { UpdateDataSourceArg, UpdateOldRecordArg, UpdateNewRecordArg, UpdateAllArg };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            var collectionType = argTypes[0];

            if (!collectionType.IsTable)
            {
                errors.EnsureError(args[0], ErrNeedTable_Func, Name);
                fValid = false;
            }

            for (var i = 1; i <= 2; i++)
            {
                var argType = argTypes[i];
                if (!argType.IsRecord)
                {
                    fValid = false;
                    errors.EnsureError(args[i], ErrNeedRecord, args[i]);
                    continue;
                }

                if (!argType.CheckAggregateNames(collectionType, args[i], errors, context.Features, SupportsParamCoercion))
                {
                    fValid = false;
                    if (!SetErrorForMismatchedColumns(collectionType, argType, args[i], errors, context.Features))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], ErrTableDoesNotAcceptThisType);
                    }
                }
                else if (SupportsParamCoercion && !collectionType.Accepts(argType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    if (argType.TryGetCoercionSubType(collectionType.ToRecord(), out DType coercionType, out bool coercionNeeded, context.Features) && coercionNeeded)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                    }
                }
            }

            if (args.Length == 4)
            {
                if (!DType.String.Accepts(argTypes[3], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) ||
                    args[3] is not StrLitNode strNode ||
                    strNode.Value.ToUpperInvariant() != "ALL")
                {
                    fValid = false;
                    errors.EnsureError(args[3], ErrRemoveAllArg, args[3]);
                }
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
        }

        public async Task<FormulaValue> InvokeAsync(FunctionInvokeInfo invokeInfo, CancellationToken cancellationToken)
        {
            var args = invokeInfo.Args;
            var returnType = invokeInfo.ReturnType;

            var validArgs = CheckArgs(args, out FormulaValue faultyArg);

            if (!validArgs)
            {
                return faultyArg;
            }

            var arg0 = args[0];
            if (arg0 is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            if (arg0 is BlankValue)
            {
                return arg0;
            }

            if (arg0 is not TableValue tableValue)
            {
                return arg0;
            }

            if (args[1] is not RecordValue oldRecord)
            {
                return args[1];
            }

            if (args[2] is not RecordValue newRecord)
            {
                return args[2];
            }

            var updateAll = args.Count == 4 && args[3] is StringValue stringValue && stringValue.Value.ToUpperInvariant() == "ALL";
            var replacementRecord = await BuildReplacementRecordAsync(tableValue, newRecord, cancellationToken).ConfigureAwait(false);
            var result = await tableValue.UpdateAsync(oldRecord, replacementRecord, updateAll, cancellationToken).ConfigureAwait(false);

            FormulaValue output;
            if (result.IsError)
            {
                output = FormulaValue.NewError(result.Error.Errors, returnType == FormulaType.Void ? FormulaType.Void : FormulaType.Blank);
            }
            else
            {
                output = returnType == FormulaType.Void ? FormulaValue.NewVoid() : FormulaValue.NewBlank();
            }

            return output;
        }

        private static async Task<RecordValue> BuildReplacementRecordAsync(TableValue tableValue, RecordValue newRecord, CancellationToken cancellationToken)
        {
            var fields = new List<NamedValue>();

            foreach (var fieldName in tableValue.Type.FieldNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FormulaValue value;

                if (newRecord.Type.FieldNames.Contains(fieldName, StringComparer.Ordinal))
                {
                    value = await newRecord.GetFieldAsync(fieldName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    value = FormulaValue.NewBlank(tableValue.Type.GetFieldType(fieldName));
                }

                fields.Add(new NamedValue(fieldName, value));
            }

            return FormulaValue.NewRecordFromFields(tableValue.Type.ToRecord(), fields);
        }
    }
}
