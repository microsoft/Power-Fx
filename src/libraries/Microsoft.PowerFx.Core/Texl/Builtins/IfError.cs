// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IfError(arg1: any, [arg2: any, ...])
    internal sealed class IfErrorFunction : BuiltinFunction
    {
        public override bool IsStrict => false;
        public override bool RequiresErrorContext => true;
        public override bool IsSelfContained => true;
        public override bool HasLambdas => true;
        public override bool IsAsync => true;
        public override bool SupportsParamCoercion => true;

        public IfErrorFunction()
            : base("IfError", TexlStrings.AboutIfError, FunctionCategories.Logical, DType.Unknown, 0, 2, int.MaxValue)
        {
            ScopeInfo = new FunctionScopeInfo(this,
                iteratesOverScope: false,
                scopeType: DType.CreateRecord(
                    new TypedName(ErrorType.ReifiedError(), new DName("FirstError")),
                    new TypedName(ErrorType.ReifiedErrorTable(), new DName("AllErrors")),
                    new TypedName(DType.ObjNull, new DName("ErrorResult"))),
                appliesToArgument: (i => i > 0 && (i % 2 == 1)));
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
                return GetGenericSignatures(arity, TexlStrings.IfErrorArg2);
            return base.GetSignatures(arity);
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

            int count = args.Length;
            nodeToCoercedTypeMap = null;

            // Check the predicates.
            bool fArgsValid = true;
            DType type = ReturnType;

            bool isBehavior = binding.IsBehavior;

            Contracts.Assert(type == DType.Unknown);
            for (int i = 0; i < count;)
            {
                TexlNode nodeArg = args[i];
                DType typeArg = argTypes[i];

                if (typeArg.IsError)
                    errors.EnsureError(args[i], TexlStrings.ErrTypeError);

                DType typeSuper = DType.Supertype(type, typeArg);

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
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, nodeArg, type);
                    else if (!isBehavior || !IsArgTypeInconsequential(nodeArg))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, nodeArg, TexlStrings.ErrBadType_ExpectedType_ProvidedType,
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
                    i--;
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

            CallNode call = arg.Parent.Parent.AsCall().VerifyValue();

            // Pattern: OnSelect = IfError(arg1, arg2, ... argK)
            // Pattern: OnSelect = IfError(arg1, IfError(arg1, arg2,...), ... argK)
            // ...etc.
            CallNode ancestor = call;
            while (ancestor.Head.Name == Name)
            {
                if (ancestor.Parent == null && ancestor.Args.Children.Length > 0)
                    return true;

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
                        return true;

                    // A chain nested within a larger non-call structure.
                    if (!(chainNode.Parent is ListNode) || !(chainNode.Parent.Parent is CallNode))
                        return false;

                    // Only the last chain segment is consequential.
                    int numSegments = chainNode.Children.Length;
                    if (numSegments > 0 && !arg.InTree(chainNode.Children[numSegments - 1]))
                        return true;
                    // The node is in the last segment of a chain nested within a larger invocation.
                    ancestor = chainNode.Parent.Parent.AsCall();
                    continue;
                }

                // Walk up the parent chain to the outer invocation.
                if (!(ancestor.Parent is ListNode) || !(ancestor.Parent.Parent is CallNode))
                    return false;

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
