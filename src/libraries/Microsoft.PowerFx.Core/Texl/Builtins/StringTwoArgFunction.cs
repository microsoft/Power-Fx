// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class StringTwoArgFunction : BuiltinFunction
    {
        public override bool UseParentScopeForArgumentSuggestions => true;
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public StringTwoArgFunction(string name, TexlStrings.StringGetter description)
            : this(name, description, DType.Boolean)
        { }

        public StringTwoArgFunction(string name, TexlStrings.StringGetter description, DType returnType)
            : base(name, description, FunctionCategories.Text, returnType, 0, 2, 2, DType.String, DType.String)
        { }

        protected bool IsRowScopedServerDelegatableHelper(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
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
            Contracts.Assert(args.Length == MinArity);

            if (binding.IsRowScope(args[1]))
            {
                binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, args[1], TexlStrings.SuggestRemoteExecutionHint_StringMatchSecondParam, Name);
                return false;
            }

            foreach (var arg in args)
            {
                var argKind = arg.VerifyValue().Kind;
                switch (argKind)
                {
                case NodeKind.FirstName:
                    var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
                    if (!firstNameStrategy.IsValidFirstNameNode(arg.AsFirstName(), binding, null))
                        return false;
                    break;
                case NodeKind.Call:
                    if (!metadata.IsDelegationSupportedByTable(FunctionDelegationCapability))
                        return false;

                    var cNodeStrategy = GetCallNodeDelegationStrategy();
                    if (!cNodeStrategy.IsValidCallNode(arg.AsCall(), binding, metadata))
                        return false;
                    break;
                case NodeKind.StrLit:
                    break;
                case NodeKind.DottedName:
                    {
                        var dottedStrategy = GetDottedNameNodeDelegationStrategy();
                        return dottedStrategy.IsValidDottedNameNode(arg.AsDottedName(), binding, metadata, null);
                    }
                default:
                    return false;
                }
            }

            return true;
        }

        public override bool HasSuggestionsForParam(int index)
        {
            return index == 0;
        }
    }
}