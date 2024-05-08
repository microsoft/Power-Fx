﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IfError(arg1: any, [arg2: any, ...])
    internal sealed class IfErrorFunction : BuiltinFunction
    {
        public override bool IsStrict => false;

        public override bool IsSelfContained => true;

        public override bool HasLambdas => true;

        public override bool IsAsync => true;

        public IfErrorFunction()
            : base("IfError", TexlStrings.AboutIfError, FunctionCategories.Logical, DType.Unknown, 0, 2, int.MaxValue)
        {
            ScopeInfo = new FunctionScopeInfo(
                this,
                iteratesOverScope: false,
                scopeType: DType.CreateRecord(
                    new TypedName(ErrorType.ReifiedError(), new DName("FirstError")),
                    new TypedName(ErrorType.ReifiedErrorTable(), new DName("AllErrors")),
                    new TypedName(DType.ObjNull, new DName("ErrorResult"))),
                appliesToArgument: i => i > 0 && (i % 2 == 1));

            // IfError(value1, fallback1, value2, fallback2, ..., valueN, [fallbackN], ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 4, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 8);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IfErrorArg1, TexlStrings.IfErrorArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetOverloadsIfError(arity);
            }

            return base.GetSignatures(arity);
        }

        // Gets the overloads for the IfError function for the specified arity.
        // IfError is special because it doesn't have a small number of overloads
        // since its max arity is int.MaxSize.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsIfError(int arity)
        {
            Contracts.Assert(arity >= 3);

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength + (arity & 1) : arity;
            var signature = new TexlStrings.StringGetter[argCount];
            var fOdd = (argCount & 1) != 0;
            var cargCur = fOdd ? argCount - 1 : argCount;

            for (var iarg = 0; iarg < cargCur; iarg += 2)
            {
                signature[iarg] = TexlStrings.IfErrorArg1;
                signature[iarg + 1] = TexlStrings.IfErrorArg2;
            }

            if (fOdd)
            {
                signature[cargCur] = TexlStrings.IfErrorArg1;
            }

            argCount++;

            return new[] { signature };
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
            
            if (context.Features.PowerFxV1CompatibilityRules)
            {
                return CheckTypesLatest(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            }
            else
            {
                return CheckTypesLegacy(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            }
        }

        public bool CheckTypesLegacy(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var count = args.Length;
            nodeToCoercedTypeMap = null;

            // Check the predicates.
            var fArgsValid = true;
            var type = ReturnType;

            var isBehavior = context.AllowsSideEffects;

            Contracts.Assert(type == DType.Unknown);
            for (var i = 0; i < count;)
            {
                var nodeArg = args[i];
                var typeArg = argTypes[i];

                if (typeArg.IsError)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrTypeError);
                }

                // In an IfError expression, not all expressions can be returned to the caller:
                // - If there is an even number of arguments, only the fallbacks or the last
                //   value (the next-to-last argument) can be returned:
                //   IfError(v1, f1, v2, f2, v3, f3) --> possible values to be returned are f1, f2, f3 or v3
                // - If there is an odd number of arguments, only the fallbacks or the last
                //   value (the last argument) can be returned:
                //   IfError(v1, f1, v2, f2, v3) --> possible values to be returned are f1, f2 or v3
                var typeCanBeReturned = (i % 2) == 1 || i == ((count % 2) == 0 ? (count - 2) : (count - 1));

                if (typeCanBeReturned)
                {
                    // Let's check if it matches the other types that can be returned
                    var typeSuper = DType.Supertype(type, typeArg, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);

                    if (!typeSuper.IsError)
                    {
                        type = typeSuper;
                    }
                    else if (type.Kind == DKind.Unknown)
                    {
                        // One of the args is also of unknown type, so we can't resolve the type of IfError
                        type = typeSuper;
                        fArgsValid = false;
                    }
                    else if (!type.IsError)
                    {
                        // Types don't resolve normally, coercion needed
                        if (typeArg.CoercesTo(type, aggregateCoercion: true, isTopLevelCoercion: false, context.Features))
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
                }

                // If there are an odd number of args, the last arg also participates.
                i += 2;
                if (i == count)
                {
                    i--;
                }
            }

            returnType = type;
            return fArgsValid;
        }

        public bool CheckTypesLatest(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var count = args.Length;
            nodeToCoercedTypeMap = null;

            var fArgsValid = true;

            /*
            If we were to write IfError(v1, f1, v2, f2, ..., vn, fn) in a try...catch way, it would look something like:
                
            var result
            try {
                result = v1();
            } catch {
                return f1();
            }
            try {
                result = v2();
            } catch {
                return f2();
            }
            ...
            try {
                result = vn();
            } catch {
                return fn();  // omit this last catch if there is an odd number of arguments
            }
            return result;

            In this case, we prefer the type of the last v_i as the return type.
            Possible return values are:
            Even case: f1, f2, ..., vn, fn
            Odd case: f1, f2, ..., vn
            */

            var possibleResults = new List<(TexlNode node, DType type)>();
            var lastValueNode = (count % 2 == 0) ? count - 2 : count - 1;
            possibleResults.Add((args[lastValueNode], argTypes[lastValueNode]));
            for (var i = 1; i < count; i += 2)
            {
                // Possible fallback results
                possibleResults.Add((args[i], argTypes[i]));
            }

            if (!IfFunction.TryDetermineReturnTypePowerFxV1CompatRules(possibleResults, errors, context.Features, ref nodeToCoercedTypeMap, out var type))
            {
                fArgsValid = false;
            }

            // Update the return type based on the specified invocation args.
            returnType = type;
            return fArgsValid;
        }

        // In behavior properties, the arg type is irrelevant if nothing actually depends
        // on the output type of IfError (see If.cs, Switch.cs)
        private bool IsArgTypeInconsequential(TexlNode arg)
        {
            Contracts.AssertValue(arg);
            Contracts.Assert(arg.Parent is ListNode);
            Contracts.Assert(arg.Parent.Parent is CallNode);
            Contracts.Assert(arg.Parent.Parent.AsCall().Head.Name == Name);

            var call = arg.Parent.Parent.AsCall().VerifyValue();

            // Pattern: OnSelect = IfError(arg1, arg2, ... argK)
            // Pattern: OnSelect = IfError(arg1, IfError(arg1, arg2,...), ... argK)
            // ...etc.
            var ancestor = call;
            while (ancestor.Head.Name == Name)
            {
                if (ancestor.Parent == null && ancestor.Args.Children.Count > 0)
                {
                    return true;
                }

                // Deal with the possibility that the ancestor may be contributing to a chain.
                // This also lets us cover the following patterns:
                // Pattern: OnSelect = X; IfError(arg1, arg2); Y; Z
                // Pattern: OnSelect = X; IfError(arg1;arg11;...;arg1k, arg2;arg21;...;arg2k); Y; Z
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
                    var numSegments = chainNode.Children.Count;
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

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            return index > 0;
        }
    }
}
