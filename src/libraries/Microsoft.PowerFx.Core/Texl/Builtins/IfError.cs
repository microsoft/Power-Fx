// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
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

        public override bool SupportsParamCoercion => true;

        public override bool CheckTypesAndSemanticsOnly => true;

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
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IfErrorArg1, TexlStrings.IfErrorArg2 };
            yield return new[] { TexlStrings.IfErrorArg1, TexlStrings.IfErrorArg2, TexlStrings.IfErrorArg2 };
            yield return new[] { TexlStrings.IfErrorArg1, TexlStrings.IfErrorArg2, TexlStrings.IfErrorArg2, TexlStrings.IfErrorArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.IfErrorArg2);
            }

            return base.GetSignatures(arity);
        }

        protected override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
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
                    var typeSuper = DType.Supertype(type, typeArg);

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
                if (ancestor.Parent == null && ancestor.Args.Children.Length > 0)
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

        public override bool IsLambdaParam(int index)
        {
            return index > 0;
        }
    }
}
