// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    [ThreadSafeImmutable]
    internal sealed class EntityArgNodeValidator : IArgValidator<IExpandInfo>
    {
        public bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValue(argNode);
            Contracts.AssertValue(binding);

            entityInfo = null;
            return argNode.Kind switch
            {
                NodeKind.FirstName => TryGetEntityInfo(argNode.AsFirstName(), binding, out entityInfo),
                NodeKind.Call => TryGetEntityInfo(argNode.AsCall(), binding, out entityInfo),
                NodeKind.DottedName => TryGetEntityInfo(argNode.AsDottedName(), binding, out entityInfo),
                _ => false,
            };
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
