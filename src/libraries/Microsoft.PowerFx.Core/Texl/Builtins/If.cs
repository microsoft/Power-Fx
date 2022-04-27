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
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

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

        public override bool SupportsParamCoercion => true;

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

        public override bool IsLazyEvalParam(int index)
        {
            return index >= 1;
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(binding);
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
                fArgsValid &= CheckType(args[i], argTypes[i], DType.Boolean, errors, true, out bool withCoercion);

                if (withCoercion)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], DType.Boolean);
                }
            }

            var type = ReturnType;

            // Are we on a behavior property?
            var isBehavior = binding.BindingConfig.AllowsSideEffects;

            // Compute the result type by joining the types of all non-predicate args.
            Contracts.Assert(type == DType.Unknown);
            for (var i = 1; i < count;)
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
                    else if (!isBehavior || !IsArgTypeInconsequential(nodeArg))
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

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex > 1;
        }

        private bool IsArgTypeInconsequential(TexlNode arg)
        {
            Contracts.AssertValue(arg);
            Contracts.Assert(arg.Parent is ListNode);
            Contracts.Assert(arg.Parent.Parent is CallNode);
            Contracts.Assert(arg.Parent.Parent.AsCall().Head.Name == Name);

            var call = arg.Parent.Parent.AsCall().VerifyValue();

            // Pattern: OnSelect = If(cond, argT, argF)
            // Pattern: OnSelect = If(cond, arg1, cond, arg2, ..., argK, argF)
            // Pattern: OnSelect = If(cond, arg1, If(cond, argT, argF))
            // Pattern: OnSelect = If(cond, arg1, If(cond, arg2, cond, arg3, ...))
            // Pattern: OnSelect = If(cond, arg1, cond, If(cond, arg2, cond, arg3, ...), ...)
            // ...etc.
            var ancestor = call;
            while (ancestor.Head.Name == Name)
            {
                if (ancestor.Parent == null && ancestor.Args.Children.Length > 0)
                {
                    for (var i = 0; i < ancestor.Args.Children.Length; i += 2)
                    {
                        // If the given node is part of a condition arg of an outer If invocation,
                        // then it's NOT inconsequential. Note that the very last arg to an If
                        // is not a condition -- it's the "else" branch, hence the test below.
                        if (i != ancestor.Args.Children.Length - 1 && arg.InTree(ancestor.Args.Children[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                // Deal with the possibility that the ancestor may be contributing to a chain.
                // This also lets us cover the following patterns:
                // Pattern: OnSelect = X; If(cond, arg1, arg2); Y; Z
                // Pattern: OnSelect = X; If(cond, arg1;arg11;...;arg1k, arg2;arg21;...;arg2k); Y; Z
                // ...etc.
                VariadicOpNode chainNode;
                if ((chainNode = ancestor.Parent.AsVariadicOp()) != null && chainNode.Op == VariadicOp.Chain)
                {
                    // Top-level chain in a behavior rule.
                    if (chainNode.Parent == null)
                    {
                        return true;
                    }

                    // A chain nested within a larger non-call structure.
                    if (!(chainNode.Parent is ListNode) || !(chainNode.Parent.Parent is CallNode))
                    {
                        return false;
                    }

                    // Only the last chain segment is consequential.
                    var numSegments = chainNode.Children.Length;
                    if (numSegments > 0 && !arg.InTree(chainNode.Children[numSegments - 1]))
                    {
                        return true;
                    }

                    // The node is in the last segment of a chain nested within a larger invocation.
                    ancestor = chainNode.Parent.Parent.AsCall();
                    continue;
                }

                // Walk up the parent chain to the outer invocation.
                if (!(ancestor.Parent is ListNode) || !(ancestor.Parent.Parent is CallNode))
                {
                    return false;
                }

                ancestor = ancestor.Parent.Parent.AsCall();
            }

            // Exhausted all supported patterns.
            return false;
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

        private bool TryGetDSNodes(TexlBinding binding, TexlNode[] args, out IList<FirstNameNode> dsNodes)
        {
            dsNodes = new List<FirstNameNode>();

            var count = args.Count();
            for (var i = 1; i < count;)
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
