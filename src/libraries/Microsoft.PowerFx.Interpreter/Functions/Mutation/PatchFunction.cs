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
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal abstract class PatchAndValidateRecordFunctionBase : BuiltinFunction
    {
        public override bool RequiresDataSourceScope => true;

        public override bool CanSuggestInputColumns => true;

        public override bool ManipulatesCollections => true;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public PatchAndValidateRecordFunctionBase(DPath theNamespace, string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
           : base(theNamespace, name, /*localeSpecificName*/string.Empty, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public PatchAndValidateRecordFunctionBase(string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            return isValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
        }       
    }

    // Patch( DataSource, BaseRecord, ChangeRecord1 [, ChangeRecord2, … ])
    internal class PatchFunction : PatchAndValidateRecordFunctionBase
    {
        public override bool IsSelfContained => false;

        public override bool MutatesArg0 => true;

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

        public PatchFunction()
            : base("Patch", AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 3, int.MaxValue, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {            
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg };
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg };
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg, PatchChangeRecordsArg };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg);
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

            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            DType dataSourceType = argTypes[0];

            if (!dataSourceType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name);
                return false;
            }

            DType retType = dataSourceType.IsError ? DType.EmptyRecord : dataSourceType.ToRecord();

            foreach (var assocDS in dataSourceType.AssociatedDataSources)
            {
                retType = DType.AttachDataSourceInfo(retType, assocDS);
            }

            for (var i = 1; i < args.Length; i++)
            {
                DType curType = argTypes[i];
                bool isSafeToUnion = true;

                if (!curType.IsRecord)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedRecord);
                    isValid = false;
                    continue;
                }

                // Checks if all record names exist against table type and if its possible to coerce.
                bool checkAggregateNames = curType.CheckAggregateNames(dataSourceType, args[i], errors, SupportsParamCoercion, context.Features.PowerFxV1CompatibilityRules);

                isValid = isValid && checkAggregateNames;
                isSafeToUnion = checkAggregateNames;

                if (isValid && SupportsParamCoercion && !dataSourceType.Accepts(curType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    if (!curType.TryGetCoercionSubType(dataSourceType, out DType coercionType, out var coercionNeeded, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                    {
                        isValid = false;
                    }
                    else
                    {
                        if (coercionNeeded)
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                        }

                        retType = DType.Union(retType, coercionType, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
                    }
                }
                else if (isSafeToUnion)
                {
                    retType = DType.Union(retType, curType, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
                }
            }

            returnType = retType;
            return isValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);

            int skip = 2;

            MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(skip).ToArray(), argTypes.Skip(skip).ToArray(), errors);
        }
    }

    internal class PatchFunctionImpl : IFunctionImplementation
    {
        // Change records are processed in order from the beginning of the argument list to the end,
        // with later property values overriding earlier ones.
        protected static async Task<Dictionary<string, FormulaValue>> CreateRecordFromArgsDictAsync(FormulaValue[] args, int startFrom, CancellationToken cancellationToken)
        {
            var retFields = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);

            for (var i = startFrom; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg is BlankValue)
                {
                    continue;
                }
                else if (arg is RecordValue record)
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

        protected static RecordValue FieldDictToRecordValue(IReadOnlyDictionary<string, FormulaValue> fieldsDict)
        {
            var list = new List<NamedValue>();

            foreach (var field in fieldsDict)
            {
                list.Add(new NamedValue(field.Key, field.Value));
            }

            return FormulaValue.NewRecordFromFields(list);
        }

        protected static bool CheckArgs(FormulaValue[] args, out FormulaValue faultyArg)
        {
            // If any args are error, propagate up.
            foreach (var arg in args)
            {
                if (arg is ErrorValue)
                {
                    faultyArg = arg;

                    return false;
                }
            }

            faultyArg = null;

            return true;
        }

        public async Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue[] args = serviceProvider.GetService<FunctionExecutionContext>().Arguments;
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

            cancellationToken.ThrowIfCancellationRequested();
            var changeRecord = FieldDictToRecordValue(await CreateRecordFromArgsDictAsync(args, 2, cancellationToken).ConfigureAwait(false));

            var datasource = (TableValue)arg0;
            var baseRecord = (RecordValue)arg1;

            cancellationToken.ThrowIfCancellationRequested();
            var ret = await datasource.PatchAsync(baseRecord, changeRecord, cancellationToken).ConfigureAwait(false);

            return ret.ToFormulaValue();
        }
    }
}
