// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Logical negation function.
    //  Not(value:b) : b
    // Equivalent Excel function: Not.
    internal class NotFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public NotFunction()
            : base("Not", TexlStrings.AboutNot, FunctionCategories.Logical, DType.Boolean, 0, 1, 1, DType.Boolean)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LogicalFuncParam };
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Not | DelegationCapability.Filter;

        // For binary op node args, we need to use filter delegation strategy. Hence we override this method here.
        public override IOpDelegationStrategy GetOpDelegationStrategy(BinaryOp op, BinaryOpNode opNode)
        {
            Contracts.AssertValueOrNull(opNode);

            if (op == BinaryOp.In)
            {
                Contracts.AssertValue(opNode);
                Contracts.Assert(opNode.Op == op);

                return new InOpDelegationStrategy(opNode, BuiltinFunctionsCore.Filter);
            }

            return new DefaultBinaryOpDelegationStrategy(op, BuiltinFunctionsCore.Filter);
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
            var argKind = args[0].VerifyValue().Kind;

            var opStrategy = GetOpDelegationStrategy(UnaryOp.Not);
            var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
            var dottedStrategy = GetDottedNameNodeDelegationStrategy();
            var cNodeStrategy = GetCallNodeDelegationStrategy();

            switch (argKind)
            {
                case NodeKind.FirstName:
                    return firstNameStrategy.IsValidFirstNameNode(args[0].AsFirstName(), binding, opStrategy);

                case NodeKind.Call:
                    {
                        if (!opStrategy.IsOpSupportedByTable(metadata, callNode, binding))
                        {
                            return false;
                        }

                        return cNodeStrategy.IsValidCallNode(args[0].AsCall(), binding, metadata);
                    }

                case NodeKind.BinaryOp:
                    {
                        if (!opStrategy.IsOpSupportedByTable(metadata, callNode, binding))
                        {
                            return false;
                        }

                        var opNode = args[0].AsBinaryOp();
                        var binaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op, opNode);
                        return binaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, metadata, binding);
                    }

                case NodeKind.UnaryOp:
                    {
                        if (!opStrategy.IsOpSupportedByTable(metadata, callNode, binding))
                        {
                            return false;
                        }

                        var opNode = args[0].AsUnaryOpLit();
                        var unaryOpNodeValidationStrategy = GetOpDelegationStrategy(opNode.Op);
                        return unaryOpNodeValidationStrategy.IsSupportedOpNode(opNode, metadata, binding);
                    }

                case NodeKind.DottedName:
                    return dottedStrategy.IsValidDottedNameNode(args[0].AsDottedName(), binding, metadata, opStrategy);

                default:
                    return argKind == NodeKind.BoolLit;
            }
        }
    }
}
