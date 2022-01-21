// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class IsBlankFunctionBase : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Null | DelegationCapability.Filter;

        public IsBlankFunctionBase(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax)
            : base(name, description, functionCategories, returnType, maskLambdas, arityMin, arityMax)
        {
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            if (!base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap))
            {
                return false;
            }

            // Option Set values need to be checked with their own function since they have a special return for "blank" values.
            if (argTypes[0].Kind == DKind.OptionSetValue)
            {
                return false;
            }

            if (argTypes[0] is IExternalControlType controlType)
            {
                // A control will never be null. It never worked as intended.
                // We coerce the control to control.primaryOutProperty.
                var primaryOutputProperty = controlType.ControlTemplate.VerifyValue().PrimaryOutputProperty;
                Contracts.AssertValueOrNull(primaryOutputProperty);

                if (primaryOutputProperty != null)
                {
                    if (nodeToCoercedTypeMap == null)
                    {
                        nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
                    }

                    nodeToCoercedTypeMap.Add(args[0], primaryOutputProperty.GetOpaqueType());
                }
            }

            return true;
        }
    }

    // IsBlank(expression:E)
    // Equivalent Excel and DAX function: IsBlank
    internal sealed class IsBlankFunction : IsBlankFunctionBase
    {
        public const string IsBlankInvariantFunctionName = "IsBlank";

        public IsBlankFunction()
            : base(IsBlankInvariantFunctionName, TexlStrings.AboutIsBlank, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsBlankArg1 };
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            if (binding.ErrorContainer.HasErrors(callNode))
            {
                return false;
            }

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var opStrategy = GetOpDelegationStrategy(BinaryOp.Equal, null);

            if (binding.IsFullRecordRowScopeAccess(args[0]))
            {
                return GetDottedNameNodeDelegationStrategy().IsValidDottedNameNode(args[0] as DottedNameNode, binding, metadata, opStrategy);
            }

            if (args[0] is not FirstNameNode node)
            {
                var message = string.Format("Arg1 is not a firstname node, instead it is {0}", args[0].Kind);
                AddSuggestionMessageToTelemetry(message, args[0], binding);
                return false;
            }

            if (!binding.IsRowScope(node))
            {
                return false;
            }

            var firstNameNodeValidationStrategy = GetFirstNameNodeDelegationStrategy();
            return firstNameNodeValidationStrategy.IsValidFirstNameNode(node, binding, opStrategy);
        }
    }

    // IsBlank(expression:E)
    // Equivalent Excel and DAX function: IsBlank
    internal sealed class IsBlankOptionSetValueFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public IsBlankOptionSetValueFunction()
            : base("IsBlank", TexlStrings.AboutIsBlank, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1, DType.OptionSetValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsBlankArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "OptionSetValue");
        }
    }
}
