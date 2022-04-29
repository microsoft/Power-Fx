// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    [ThreadSafeImmutable]
    internal sealed class DataSourceArgNodeValidator : IArgValidator<IList<FirstNameNode>>
    {
        public bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(argNode);
            Contracts.AssertValue(binding);

            dsNodes = new List<FirstNameNode>();
            switch (argNode.Kind)
            {
                case NodeKind.FirstName:
                    if (TryGetDsNode(argNode.AsFirstName(), binding, out var dsNode))
                    {
                        dsNodes.Add(dsNode);
                    }

                    break;
                case NodeKind.Call:
                    return TryGetDsNodes(argNode.AsCall(), binding, out dsNodes);
                case NodeKind.DottedName:
                    return TryGetDsNode(argNode.AsDottedName(), binding, out dsNodes);
            }

            return dsNodes.Count > 0;
        }

        private bool TryGetDsNodes(CallNode callNode, TexlBinding binding, out IList<FirstNameNode> dsInfos)
        {
            Contracts.AssertValueOrNull(callNode);
            Contracts.AssertValue(binding);

            dsInfos = new List<FirstNameNode>();
            if (callNode == null || !binding.GetType(callNode).IsAggregate)
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

            return function.TryGetDataSourceNodes(callNode, binding, out dsInfos);
        }

        private bool TryGetDsNode(FirstNameNode firstName, TexlBinding binding, out FirstNameNode dsNode)
        {
            Contracts.AssertValueOrNull(firstName);
            Contracts.AssertValue(binding);

            dsNode = null;
            if (firstName == null || !binding.GetType(firstName).IsTable)
            {
                return false;
            }

            var firstNameInfo = binding.GetInfo(firstName);
            if (firstNameInfo == null || firstNameInfo.Kind != BindKind.Data)
            {
                return false;
            }

            if (binding.EntityScope == null || !binding.EntityScope.TryGetEntity(firstNameInfo.Name, out IExternalDataSource _))
            {
                return false;
            }

            dsNode = firstName;
            return true;
        }

        private bool TryGetDsNode(DottedNameNode dottedNameNode, TexlBinding binding, out IList<FirstNameNode> dsNode)
        {
            Contracts.AssertValueOrNull(dottedNameNode);
            Contracts.AssertValue(binding);

            dsNode = null;
            if (dottedNameNode == null || !binding.HasExpandInfo(dottedNameNode))
            {
                return false;
            }

            return TryGetValidValue(dottedNameNode.Left, binding, out dsNode);
        }
    }
}
