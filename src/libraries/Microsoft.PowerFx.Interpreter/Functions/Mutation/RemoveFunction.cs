// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Functions
{
    internal abstract class RemoveFunctionBase : BuiltinFunction
    {
        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        public override bool CanSuggestInputColumns => true;

        public override bool ManipulatesCollections => true;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public override bool MutatesArg0 => true;

        public override bool IsLazyEvalParam(int index, Features features)
        {
            // First argument to mutation functions is Lazy for datasources that are copy-on-write.
            // If there are any side effects in the arguments, we want those to have taken place before we make the copy.
            return index == 0;
        }

        public RemoveFunctionBase(DPath theNamespace, string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(theNamespace, name, /*localeSpecificName*/string.Empty, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public RemoveFunctionBase(string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
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
    }

    internal class RemoveFunction : RemoveFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex == 1)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public RemoveFunction()
        : base("Remove", AboutRemove, FunctionCategories.Table | FunctionCategories.Behavior, DType.Unknown, 0, 2, int.MaxValue, DType.EmptyTable, DType.EmptyRecord)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { RemoveDataSourceArg, RemoveRecordsArg };
            yield return new[] { RemoveDataSourceArg, RemoveRecordsArg, RemoveRecordsArg };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, RemoveDataSourceArg, RemoveRecordsArg, RemoveRecordsArg);
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

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
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

            if (arg0 is BlankValue)
            {
                return arg0;
            }

            var argCount = args.Count();
            var lastArg = args.Last() as FormulaValue;
            var all = false;
            var toExclude = 1;

            if (argCount >= 3 && DType.String.Accepts(lastArg.Type._type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: true))
            {
                var lastArgValue = (string)lastArg.ToObject();

                if (lastArgValue.ToUpperInvariant() == "ALL")
                {
                    all = true;
                    toExclude = 2;
                }
            }

            var datasource = (TableValue)arg0;
            var recordsToRemove = args.Skip(1).Take(args.Length - toExclude);

            cancellationToken.ThrowIfCancellationRequested();
            var ret = await datasource.RemoveAsync(recordsToRemove, all, cancellationToken).ConfigureAwait(false);

            // If the result is an error, propagate it up. else return blank.
            FormulaValue result;
            if (ret.IsError)
            {
                result = FormulaValue.NewError(ret.Error.Errors, FormulaType.Blank);
            }
            else
            {
                result = FormulaValue.NewVoid();
            }

            return result;
        }
    }
}
