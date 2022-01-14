// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    internal sealed class EntityArgNodeValidator : IArgValidator<IExpandInfo>
    {
        public bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValue(argNode);
            Contracts.AssertValue(binding);

            entityInfo = null;
            switch (argNode.Kind)
            {
                case NodeKind.FirstName:
                    return TryGetEntityInfo(argNode.AsFirstName(), binding, out entityInfo);
                case NodeKind.Call:
                    return TryGetEntityInfo(argNode.AsCall(), binding, out entityInfo);
                case NodeKind.DottedName:
                    return TryGetEntityInfo(argNode.AsDottedName(), binding, out entityInfo);
            }

            return false;
        }

        private bool TryGetEntityInfo(CallNode callNode, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValueOrNull(callNode);
            Contracts.AssertValue(binding);

            entityInfo = null;
            if (callNode == null || !binding.GetType(callNode).IsTable)
            {
                return false;
            }

            var callInfo = binding.GetInfo(callNode);
            if (callInfo == null)
            {
                return false;
            }

            var function = callInfo.Function;
            if (function == null)
            {
                return false;
            }

            return function.TryGetEntityInfo(callNode, binding, out entityInfo);
        }

        private bool TryGetEntityInfo(FirstNameNode firstName, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValueOrNull(firstName);
            Contracts.AssertValue(binding);

            entityInfo = null;
            if (firstName == null || !binding.GetType(firstName).IsTable)
            {
                return false;
            }

            return binding.TryGetEntityInfo(firstName, out entityInfo);
        }

        private bool TryGetEntityInfo(DottedNameNode dottedNameNode, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValueOrNull(dottedNameNode);
            Contracts.AssertValue(binding);

            entityInfo = null;
            if (dottedNameNode == null || !binding.HasExpandInfo(dottedNameNode))
            {
                return false;
            }

            return binding.TryGetEntityInfo(dottedNameNode, out entityInfo);
        }
    }
}
