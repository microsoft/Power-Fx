// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    [ThreadSafeImmutable]
    internal sealed class DelegatableDataSourceInfoValidator : IArgValidator<IExternalDataSource>
    {
        public bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValue(argNode);
            Contracts.AssertValue(binding);

            dsInfo = null;
            switch (argNode.Kind)
            {
                case NodeKind.FirstName:
                    return TryGetDsInfo(argNode.AsFirstName(), binding, out dsInfo);
                case NodeKind.Call:
                    return TryGetDsInfo(argNode.AsCall(), binding, out dsInfo);
                case NodeKind.DottedName:
                    return TryGetDsInfo(argNode.AsDottedName(), binding, out dsInfo);
                case NodeKind.As:
                    return TryGetValidValue(argNode.AsAsNode().Left, binding, out dsInfo);
            }

            return false;
        }

        private bool TryGetDsInfo(CallNode callNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValueOrNull(callNode);
            Contracts.AssertValue(binding);

            dsInfo = null;
            if (callNode == null || !binding.IsDelegatable(callNode) || !binding.GetType(callNode).IsTable)
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

            var success = function.TryGetDataSource(callNode, binding, out var external);
            dsInfo = (IExternalDataSource)external;
            return success;
        }

        private bool TryGetDsInfo(FirstNameNode firstName, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValueOrNull(firstName);
            Contracts.AssertValue(binding);

            dsInfo = null;
            if (firstName == null || !binding.GetType(firstName).IsTable)
            {
                return false;
            }

            var firstNameInfo = binding.GetInfo(firstName);
            if (firstNameInfo == null || firstNameInfo.Kind != BindKind.Data)
            {
                return false;
            }

            return binding.EntityScope != null &&
                binding.EntityScope.TryGetEntity(firstNameInfo.Name, out dsInfo);
        }

        private bool TryGetDsInfo(DottedNameNode dottedNameNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValueOrNull(dottedNameNode);
            Contracts.AssertValue(binding);

            dsInfo = null;
            if (dottedNameNode == null || !binding.HasExpandInfo(dottedNameNode))
            {
                return false;
            }

            binding.TryGetEntityInfo(dottedNameNode, out var info).Verify();
            dsInfo = info.ParentDataSource;
            return true;
        }
    }
}
