// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter
{
    // The CollectFunction class was copied from PowerApss.
    // Implementation of a Set function which just chains to 
    // RecalcEngine.UpdateVariable().
    // Set has no return value. 
    // Whereas PowerApps' Set() will implicitly define arg0,
    //  this Set() requires arg0 was already defined and has a type.
    //
    // Called as:
    //   Set(var,newValue)
    internal class CollectFunction : BuiltinFunction, IAsyncTexlFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        protected virtual bool IsScalar => false;

        public override bool CanSuggestInputColumns => true;

        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex == 1)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectFunction"/> class.
        /// To be consumed by ClearCollect function.
        /// </summary>
        protected CollectFunction(string name, TexlStrings.StringGetter description)
            : base(name, description, FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, 2, DType.EmptyTable, DType.EmptyRecord)
        {
        }

        public CollectFunction()
        : base(
              "Collect",
              TexlStrings.AboutCollect,
              FunctionCategories.Behavior,
              DType.EmptyRecord,
              0,
              2,
              2, // Not handling multiple arguments for now
              DType.EmptyTable,
              DType.EmptyRecord) 
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CollectDataSourceArg, TexlStrings.CollectRecordArg };
        }

        public virtual DType GetCollectedType(DType argType)
        {
            Contracts.Assert(argType.IsValid);

            return argType;
        }

        // Attempt to get the unified schema of the items being collected by an invocation.
        private bool TryGetUnifiedCollectedType(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType collectedType)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = true;
            DType itemType = DType.Invalid;

            var argc = args.Length;

            for (var i = 1; i < argc; i++)
            {
                DType argType = GetCollectedType(argTypes[i]);

                // The subsequent args should all be aggregates.
                if (!argType.IsAggregate)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrBadType_Type, argType.GetKindString());
                    fValid = false;
                    continue;
                }

                // Promote the arg type to a table to facilitate unioning.
                if (!argType.IsRecord)
                {
                    argType = argType.ToRecord();
                }

                // Checks if all record names exist against table type and if its possible to coerce.
                bool checkAggregateNames = argType.CheckAggregateNames(argTypes[0], args[i], errors, context.Features, SupportsParamCoercion);
                fValid = fValid && checkAggregateNames;

                if (!itemType.IsValid)
                {
                    itemType = argType;
                }
                else
                {
                    var fUnionError = false;
                    itemType = DType.Union(ref fUnionError, itemType, argType, useLegacyDateTimeAccepts: true, context.Features);
                    if (fUnionError)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrIncompatibleTypes);
                        fValid = false;
                    }
                }

                // We only support accessing entities in collections if the collection has only 1 argument that contributes to it's type
                if (argc != 2 && itemType.ContainsDataEntityType(DPath.Root))
                {
                    fValid &= DropAllOfKindNested(ref itemType, errors, args[i], DKind.DataEntity);
                }
            }

            Contracts.Assert(!itemType.IsValid || itemType.IsRecord);
            collectedType = itemType.IsValid ? itemType : DType.EmptyRecord;
            return fValid;
        }

        // Typecheck an invocation of Collect.
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrInvalidArgs_Func, Name);
                fValid = false;
            }

            // Get the unified collected type on the RHS. This will generate appropriate
            // document errors for invalid arguments such as unsupported aggregate types.
            fValid &= TryGetUnifiedCollectedType(context, args, argTypes, errors, out DType collectedType);
            Contracts.Assert(collectedType.IsRecord);

            if (fValid)
            {
                if (!collectedType.TryGetCoercionSubType(collectionType, out DType coercionType, out var coercionNeeded, context.Features))
                {
                    fValid = false;
                }
                else
                {
                    if (coercionNeeded)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], coercionType);
                    }

                    var fError = false;

                    returnType = DType.Union(ref fError, collectionType.ToRecord(), collectedType, useLegacyDateTimeAccepts: false, context.Features, allowCoerce: true);

                    if (fError)
                    {
                        fValid = false;
                        if (!SetErrorForMismatchedColumns(collectionType, collectedType, args[1], errors, context.Features, RequireAllParamColumns))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTableDoesNotAcceptThisTypeDetailed, collectedType.GetKindString());
                        }
                    }
                }
            }

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);

            int skip = 1;

            MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(skip).ToArray(), argTypes.Skip(skip).ToArray(), errors);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        public override bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        public virtual async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            FormulaValue arg0;

            // Need to check if the Lazy first argument has been evaluated since it may have already been
            // evaluated in the ClearCollect case.
            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }
            else
            {
                arg0 = args[0];
            }

            var arg1 = args[1];

            // PA returns arg0.
            // PFx returns arg1 for now except when arg0 is anything but TableValue, return arg0 or RuntimeTypeMismatch error.
            if (arg0 is BlankValue)
            {
                return arg0;
            }
            else if (arg0 is ErrorValue)
            {
                return arg0;
            }

            if (arg0 is not TableValue)
            {
                return CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(arg0.Type));
            }

            // If arg0 is valid, then return arg1.
            if (arg1 is BlankValue)
            {
                return arg1;
            }
            else if (arg1 is ErrorValue)
            {
                return arg1;
            }

            if (arg1 is not RecordValue)
            {
                return CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(arg1.Type));
            }

            var tableValue = arg0 as TableValue;
            var recordValueToAppend = (RecordValue)arg1.MaybeShallowCopy();

            cancellationToken.ThrowIfCancellationRequested();
            var result = await tableValue.AppendAsync(recordValueToAppend, cancellationToken).ConfigureAwait(false);

            return result.ToFormulaValue();
        }
    }
}
