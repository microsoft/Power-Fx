// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // If(cond1:b, value1, [cond2:b, value2, ..., [valueFalse]])
    // Corresponding DAX functions: If, IfError, Switch
    internal sealed class IfFunction : BuiltinFunction
    {
        public override bool IsStrict => false;

        public override int SuggestionTypeReferenceParamIndex => 1;

        public override bool UsesEnumNamespace => true;

        public override bool IsSelfContained => true;

        public IfFunction()
            : base("If", TexlStrings.AboutIf, FunctionCategories.Logical, DType.Unknown, 0, 2, int.MaxValue)
        {
            // If(cond1, value1, cond2, value2, ..., condN, valueN, [valueFalse], ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 4, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 8);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.IfArgCond, TexlStrings.IfArgTrueValue };
            yield return new[] { TexlStrings.IfArgCond, TexlStrings.IfArgTrueValue, TexlStrings.IfArgElseValue };
            yield return new[] { TexlStrings.IfArgCond, TexlStrings.IfArgTrueValue, TexlStrings.IfArgCond, TexlStrings.IfArgTrueValue };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetOverloadsIf(arity);
            }

            return base.GetSignatures(arity);
        }

        public override bool IsLazyEvalParam(int index, Features features)
        {
            return index >= 1;
        }

        internal static bool TryDetermineReturnTypePowerFxV1CompatRules(
            List<(TexlNode node, DType type)> possibleResults,
            IErrorContainer errors, 
            ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap,
            out DType returnType)
        {
            returnType = null;
            var type = possibleResults[0].type;
            var fArgsValid = true;

            foreach (var (argNode, argType) in possibleResults)
            {
                if (argType.IsVoid)
                {
                    type = DType.Void;
                }
                else if (argType.IsError)
                {
                    errors.EnsureError(argNode, TexlStrings.ErrTypeError);
                    fArgsValid = false;
                }
                else if (type.Kind == DKind.ObjNull)
                {
                    // Anything goes with null
                    type = argType;
                }
                else if (argType.Kind == DKind.ObjNull)
                {
                    // ObjNull can be accepted by the current type
                }
                else if (DType.TryUnionWithCoerce(
                         type,
                         argType,
                         usePowerFxV1CompatibilityRules: true,
                         coerceToLeftTypeOnly: true,
                         out var unionType,
                         out var coercionNeeded))
                {
                    type = unionType;
                    if (coercionNeeded)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, argNode, type);
                    }
                }
                else
                {
                    // If types are incompatible, result is Void
                    type = DType.Void;
                }
            }

            returnType = type;
            return fArgsValid;
        }

        internal static bool TryDetermineReturnTypePowerFxV1CompatRulesDisabled(
            List<(TexlNode node, DType type)> possibleResults,
            IErrorContainer errors,
            ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap,
            out DType returnType)
        {
            returnType = null;
            var type = DType.Unknown;
            var fArgsValid = true;

            foreach (var (nodeArg, typeArg) in possibleResults)
            {
                var typeSuper = DType.Supertype(
                    type,
                    typeArg,
                    useLegacyDateTimeAccepts: false,
                    usePowerFxV1CompatibilityRules: false);

                if (!typeSuper.IsError)
                {
                    type = typeSuper;
                }
                else if (typeArg.IsVoid)
                {
                    type = DType.Void;
                }
                else if (typeArg.IsError)
                {
                    errors.EnsureError(nodeArg, TexlStrings.ErrTypeError);
                    fArgsValid = false;
                }
                else if (!type.IsError)
                {
                    if (typeArg.CoercesTo(type, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: false))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, nodeArg, type);
                    }
                    else
                    {
                        // If the types are incompatible, the result type is void.
                        type = DType.Void;
                    }
                }
                else if (typeArg.Kind != DKind.Unknown)
                {
                    type = typeArg;
                    fArgsValid = false;
                }
            }

            returnType = type;
            return fArgsValid;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var count = args.Length;
            nodeToCoercedTypeMap = null;

            // Check the predicates.
            var fArgsValid = true;
            for (var i = 0; i < (count & ~1); i += 2)
            {
                fArgsValid &= CheckType(context, args[i], argTypes[i], DType.Boolean, errors, true, out bool withCoercion);

                if (withCoercion)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], DType.Boolean);
                }
            }

            var type = context.Features.PowerFxV1CompatibilityRules ? argTypes[1] : ReturnType;

            var possibleResults = new List<(TexlNode node, DType type)>();
            for (var i = 1; i < count;)
            {
                possibleResults.Add((args[i], argTypes[i]));

                // If there are an odd number of args, the last arg also participates.
                i += 2;
                if (i == count)
                {
                    i--;
                }
            }

            // For pre-PowerFxV1, compute the result type by joining the types of all non-predicate args.
            // For PowerFxV1 compat rules, validate that all non-predicate args can be coerced to the first one
            if (context.Features.PowerFxV1CompatibilityRules)
            {
                if (!TryDetermineReturnTypePowerFxV1CompatRules(possibleResults, errors, ref nodeToCoercedTypeMap, out type))
                {
                    fArgsValid = false;
                }
            }
            else
            {
                if (!TryDetermineReturnTypePowerFxV1CompatRulesDisabled(possibleResults, errors, ref nodeToCoercedTypeMap, out type))
                {
                    fArgsValid = false;
                }
            }

            // Update the return type based on the specified invocation args.
            returnType = type;
            return fArgsValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex > 1;
        }

        // Gets the overloads for the If function for the specified arity.
        // If is special because it doesn't have a small number of overloads
        // since its max arity is int.MaxSize.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsIf(int arity)
        {
            Contracts.Assert(arity >= 3);

            // REVIEW ragru: What should be the number of overloads for functions like these?
            // Once we decide should we just hardcode the number instead of having the outer loop?
            const int OverloadCount = 3;

            var overloads = new List<TexlStrings.StringGetter[]>(OverloadCount);

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength + (arity & 1) : arity;
            for (var ioverload = 0; ioverload < OverloadCount; ioverload++)
            {
                var signature = new TexlStrings.StringGetter[argCount];
                var fOdd = (argCount & 1) != 0;
                var cargCur = fOdd ? argCount - 1 : argCount;

                for (var iarg = 0; iarg < cargCur; iarg += 2)
                {
                    signature[iarg] = TexlStrings.IfArgCond;
                    signature[iarg + 1] = TexlStrings.IfArgTrueValue;
                }

                if (fOdd)
                {
                    signature[cargCur] = TexlStrings.IfArgElseValue;
                }

                argCount++;
                overloads.Add(signature);
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(overloads);
        }

        public override bool TryGetDataSourceArgumentIndices(int argCount, out IEnumerable<int> dataSourceArgs)
        {
            Contracts.Assert(argCount >= MinArity);
            Contracts.Assert(argCount <= MaxArity);

            dataSourceArgs = ControlFlowFunctionHelper.GetDataSourceArgumentIndices(argCount);
            return true;
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            if (!DataSourceNodeHelper.TryGetDataSourceNodes(callNode, this, binding, out _))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var count = args.Count();

            for (var i = 1; i < count;)
            {
                if (!binding.IsPageable(args[i]))
                {
                    return false;
                }

                // If there are an odd number of args, the last arg also participates.
                i += 2;
                if (i == count)
                {
                    i--;
                }
            }

            return true;
        }
    }
}
