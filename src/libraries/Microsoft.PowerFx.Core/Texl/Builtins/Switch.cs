// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    /// <summary>
    /// SwitchFunction evaluates first argument (clled the expression) against a list of values,
    /// and returns the result corresponding to the first matching value. If there is no match,
    /// an optional default value(which is last argument if number of arguments are even) is returned.
    /// Syntax:
    /// Switch(Value to switch,Value to match 1...[2-N], Value to return for match1...[2-N], [Value to return if there's no match]).
    /// </summary>
    internal sealed class SwitchFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // Note, switch has a very custom checkinvocation implementation
        // We do not support coercion for the 1st param, or the match params, only the result params. 
        public override bool SupportsParamCoercion => true;

        public SwitchFunction()
            : base("Switch", TexlStrings.AboutSwitch, FunctionCategories.Logical, DType.Unknown, 0, 3, int.MaxValue)
        {
            // If(cond1, value1, cond2, value2, ..., condN, valueN, [valueFalse], ...)
            // Switch(switch_value, match_value1, match_result1, match_value2, match_result2, ..., match_valueN, match_resultN, [default_result], ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 5, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 9);
        }

        // Return all signatures for switch function.
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads.
            yield return new[] { TexlStrings.SwitchExpression, TexlStrings.SwitchCaseExpr, TexlStrings.SwitchCaseArg };
            yield return new[] { TexlStrings.SwitchExpression, TexlStrings.SwitchCaseExpr, TexlStrings.SwitchCaseArg, TexlStrings.SwitchDefaultReturn };
        }

        public override bool IsLazyEvalParam(int index)
        {
            return index > 0;
        }

        // Return all signatures for switch function with at most 'arity' parameters.
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity < 5)
            {
                return base.GetSignatures(arity);
            }

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength + (arity & 1 ^ 1) : arity;
            var signature = new TexlStrings.StringGetter[argCount];
            var fEven = (argCount & 1) == 0;
            var cargCur = fEven ? argCount - 1 : argCount;
            signature[0] = TexlStrings.SwitchExpression;
            for (var iarg = 1; iarg < cargCur; iarg += 2)
            {
                signature[iarg] = TexlStrings.SwitchCaseExpr;
                signature[iarg + 1] = TexlStrings.SwitchCaseArg;
            }

            if (fEven)
            {
                signature[cargCur] = TexlStrings.SwitchDefaultReturn;
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(new[] { signature });
        }

        // Type check an invocation of the function with the specified args (and their corresponding types).
        // Return true if everything aligns, false otherwise.
        // This override does not post any document errors (i.e. it performs the typechecks quietly).
        public override bool CheckTypes(BindingConfig config, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(config);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var count = args.Length;

            // Check the switch expression type matches all case expression types in list.
            var fArgsValid = true;
            for (var i = 1; i < count - 1; i += 2)
            {
                if (!argTypes[0].Accepts(argTypes[i]) && !argTypes[i].Accepts(argTypes[0]))
                {
                    // Type mismatch; using CheckType to fill the errors collection
                    var validExpectedType = CheckType(args[i], argTypes[i], argTypes[0], errors, coerceIfSupported: false, out bool _);
                    if (validExpectedType)
                    {
                        // Check on the opposite direction
                        validExpectedType = CheckType(args[0], argTypes[0], argTypes[i], errors, coerceIfSupported: false, out bool _);
                    }

                    fArgsValid &= validExpectedType;
                }
            }

            var type = ReturnType;
            nodeToCoercedTypeMap = null;

            // Are we on a behavior property?
            var isBehavior = config.AllowsSideEffects;

            // Compute the result type by joining the types of all non-predicate args.
            Contracts.Assert(type == DType.Unknown);
            for (var i = 2; i < count;)
            {
                var nodeArg = args[i];
                var typeArg = argTypes[i];
                if (typeArg.IsError)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrTypeError);
                }

                var typeSuper = DType.Supertype(type, typeArg);

                if (!typeSuper.IsError)
                {
                    type = typeSuper;
                }
                else if (type.Kind == DKind.Unknown)
                {
                    type = typeSuper;
                    fArgsValid = false;
                }
                else if (!type.IsError)
                {
                    if (typeArg.CoercesTo(type))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, nodeArg, type);
                    }
                    else if (!isBehavior)
                    {
                        errors.EnsureError(
                            DocumentErrorSeverity.Severe,
                            nodeArg,
                            TexlStrings.ErrBadType_ExpectedType_ProvidedType,
                            type.GetKindString(),
                            typeArg.GetKindString());
                        fArgsValid = false;
                    }
                }
                else if (typeArg.Kind != DKind.Unknown)
                {
                    type = typeArg;
                    fArgsValid = false;
                }

                // If there are an odd number of args, the last arg also participates.
                i += 2;
                if (i == count)
                {
                    i--;
                }
            }

            // Update the return type based on the specified invocation args.
            returnType = type;

            return fArgsValid;
        }

        private bool TryGetDSNodes(TexlBinding binding, TexlNode[] args, out IList<FirstNameNode> dsNodes)
        {
            dsNodes = new List<FirstNameNode>();

            var count = args.Count();
            for (var i = 2; i < count;)
            {
                var nodeArg = args[i];

                if (ArgValidators.DataSourceArgNodeValidator.TryGetValidValue(nodeArg, binding, out var tmpDsNodes))
                {
                    foreach (var node in tmpDsNodes)
                    {
                        dsNodes.Add(node);
                    }
                }

                // If there are an odd number of args, the last arg also participates.
                i += 2;
                if (i == count)
                {
                    i--;
                }
            }

            return dsNodes.Any();
        }

        public override bool TryGetDataSourceNodes(CallNode callNode, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dsNodes = new List<FirstNameNode>();
            if (callNode.Args.Count < 2)
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            return TryGetDSNodes(binding, args, out dsNodes);
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            if (!TryGetDataSourceNodes(callNode, binding, out var dsNodes))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var count = args.Count();

            for (var i = 2; i < count;)
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
