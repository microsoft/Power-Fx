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
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Functions.Mutation
{
    internal class UpdateFunction : PatchAndValidateRecordFunctionBase, IAsyncTexlFunction
    {
        public UpdateFunction()
            : base("Update", AboutUpdate, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 3, 4, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override bool MutatesArg0 => true;

        public override bool IsSelfContained => false;

        protected static async Task<Dictionary<string, FormulaValue>> CreateRecordFromArgsDictAsync(FormulaValue[] args, int startFrom, int exclude, CancellationToken cancellationToken)
        {
            var retFields = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);

            for (var i = startFrom; i < args.Length - exclude; i++)
            {
                var arg = args[i];

                if (arg is RecordValue record)
                {
                    await foreach (var field in record.GetFieldsAsync(cancellationToken).ConfigureAwait(false))
                    {
                        retFields[field.Name] = field.Value;
                    }
                }
                else
                {
                    throw new ArgumentException($"Can't handle {arg.Type} argument type.");
                }
            }

            return retFields;
        }

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex == 1 || argIndex == 2)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public override bool IsLazyEvalParam(int index, Features features)
        {
            // First argument to mutation functions is Lazy for datasources that are copy-on-write.
            // If there are any side effects in the arguments, we want those to have taken place before we make the copy.
            return index == 0;
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { UpdateDataSourceArg, UpdateBaseRecordArg, UpdateChangeRecordArg };
            yield return new[] { UpdateDataSourceArg, UpdateBaseRecordArg, UpdateChangeRecordArg,  };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, UpdateDataSourceArg, UpdateBaseRecordArg, UpdateChangeRecordArg);
            }

            return base.GetSignatures(arity);
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

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(args[0], ErrNeedTable_Func, Name);
                fValid = false;
            }

            var argCount = argTypes.Length;

            for (var i = 1; i < argCount; i++)
            {
                DType argType = argTypes[i];

                // The subsequent args should all be records.
                if (!argType.IsRecord)
                {
                    // The last arg may be the optional "ALL" parameter.
                    if (argCount >= 3 && i == argCount - 1 && DType.String.Accepts(argType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        var strNode = (StrLitNode)args[i];

                        if (strNode.Value.ToUpperInvariant() != "ALL")
                        {
                            fValid = false;
                            errors.EnsureError(args[i], ErrRemoveAllArg, args[i]);
                        }

                        continue;
                    }

                    fValid = false;
                    errors.EnsureError(args[i], ErrNeedRecord, args[i]);
                    continue;
                }

                var collectionAcceptsRecord = collectionType.Accepts(argType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
                var recordAcceptsCollection = argType.ToTable().Accepts(collectionType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);

                var featuresWithPFxV1RulesDisabled = new Features(context.Features) { PowerFxV1CompatibilityRules = false };
                bool checkAggregateNames = argType.CheckAggregateNames(collectionType, args[i], errors, featuresWithPFxV1RulesDisabled, SupportsParamCoercion);

                // The item schema should be compatible with the collection schema.
                if (!checkAggregateNames)
                {
                    fValid = false;
                    if (!SetErrorForMismatchedColumns(collectionType, argType, args[i], errors, context.Features))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], ErrTableDoesNotAcceptThisType);
                    }
                }
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.ObjNull : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);

            int skip = 2;

            MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(skip).ToArray(), argTypes.Skip(skip).ToArray(), errors);
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var validArgs = CheckArgs(args, out FormulaValue faultyArg);

            if (!validArgs)
            {
                return faultyArg;
            }

            var arg0lazy = (LambdaFormulaValue)args[0];
            var arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            var arg1 = args[1];

            if (arg0 is BlankValue)
            {
                return arg0;
            }
            else if (arg0 is ErrorValue)
            {
                return arg0;
            }

            if (arg1 is BlankValue)
            {
                return arg1;
            }

            var argCount = args.Count();
            var lastArg = args.Last() as FormulaValue;
            var all = false;
            var toExclude = 0;

            if (argCount >= 4 && DType.String.Accepts(lastArg.Type._type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: true))
            {
                var lastArgValue = (string)lastArg.ToObject();

                if (lastArgValue.ToUpperInvariant() == "ALL")
                {
                    all = true;
                    toExclude = 1;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var changeRecord = FieldDictToRecordValue(await CreateRecordFromArgsDictAsync(args, 2, toExclude, cancellationToken).ConfigureAwait(false));

            var datasource = (TableValue)arg0;
            var baseRecord = (RecordValue)arg1;

            cancellationToken.ThrowIfCancellationRequested();
            var ret = await datasource.UpdateAsync(baseRecord, changeRecord, all, cancellationToken).ConfigureAwait(false);

            return ret.ToFormulaValue();
        }
    }
}
