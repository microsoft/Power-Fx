// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

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
            return argNode.Kind switch
            {
                NodeKind.FirstName => TryGetDsInfo(argNode.AsFirstName(), binding, out dsInfo),
                NodeKind.Call => TryGetDsInfo(argNode.AsCall(), binding, out dsInfo),
                NodeKind.DottedName => TryGetDsInfo(argNode.AsDottedName(), binding, out dsInfo),
                NodeKind.As => TryGetValidValue(argNode.AsAsNode().Left, binding, out dsInfo),
                _ => false,
            };
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
            dsInfo = external;
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
