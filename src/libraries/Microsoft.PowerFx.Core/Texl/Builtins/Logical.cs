// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Variadic logical operator functions:
    //  And(cond:b, cond:b, ...) : b
    //  Or(cond:b, cond:b, ...) : b
    // Equivalent Excel functions: And, Or.
    internal sealed class VariadicLogicalFunction : BuiltinFunction
    {
        public override bool IsStrict => false;

        public override bool IsSelfContained => true;

        public override bool RequiresErrorContext => true;

        public override bool SupportsParamCoercion => true;

        internal readonly bool _isAnd;

        public VariadicLogicalFunction(bool isAnd)
            : base(isAnd ? "And" : "Or", isAnd ? TexlStrings.AboutAnd : TexlStrings.AboutOr, FunctionCategories.Logical, DType.Boolean, 0, 0, int.MaxValue, DType.Boolean)
        {
            _isAnd = isAnd;
        }

        public override bool IsLazyEvalParam(int index)
        {
            return index > 0;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.LogicalFuncParam };
            yield return new[] { TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam };
            yield return new[] { TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam };
        }

        // And / Or functions are implicit here if filter capability is supported. Hence we just declare capability filter here.
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Filter;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.LogicalFuncParam);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
            var count = args.Length;

            // Check the args.
            var fArgsValid = true;
            for (var i = 0; i < count; i++)
            {
                fArgsValid &= CheckType(args[i], argTypes[i], DType.Boolean, errors, out var matchedWithCoercion);
                if (matchedWithCoercion)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], DType.Boolean);
                }
            }

            returnType = ReturnType;

            return fArgsValid;
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            if (binding.ErrorContainer.HasErrors(callNode) ||
                !CheckArgsCount(callNode, binding) ||
                !binding.IsRowScope(callNode))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            Contracts.Assert(args.Length >= MinArity);

            var funcDelegationCapability = FunctionDelegationCapability | (_isAnd ? DelegationCapability.And : DelegationCapability.Or);
            if (!metadata.IsDelegationSupportedByTable(funcDelegationCapability))
            {
                return false;
            }

            foreach (var arg in args)
            {
                var argKind = arg.VerifyValue().Kind;
                switch (argKind)
                {
                    case NodeKind.FirstName:
                        {
                            var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
                            if (!firstNameStrategy.IsValidFirstNameNode(arg.AsFirstName(), binding, null))
                            {
                                return false;
                            }

                            break;
                        }

                    case NodeKind.Call:
                        {
                            var cNodeStrategy = GetCallNodeDelegationStrategy();
                            if (!cNodeStrategy.IsValidCallNode(arg.AsCall(), binding, metadata))
                            {
                                SuggestDelegationHint(arg, binding);
                                return false;
                            }

                            break;
                        }

                    case NodeKind.DottedName:
                        {
                            var dottedStrategy = GetDottedNameNodeDelegationStrategy();
                            if (!dottedStrategy.IsValidDottedNameNode(arg.AsDottedName(), binding, metadata, null))
                            {
                                SuggestDelegationHint(arg, binding);
                                return false;
                            }

                            break;
                        }

                    case NodeKind.BinaryOp:
                        {
                            var opNode = arg.AsBinaryOp();
                            var binaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op, opNode);
                            if (!binaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, metadata, binding))
                            {
                                SuggestDelegationHint(arg, binding);
                                return false;
                            }

                            break;
                        }

                    case NodeKind.UnaryOp:
                        {
                            var opNode = arg.AsUnaryOpLit();
                            var unaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op);
                            if (!unaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, metadata, binding))
                            {
                                SuggestDelegationHint(arg, binding);
                                return false;
                            }

                            break;
                        }

                    case NodeKind.BoolLit:
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
