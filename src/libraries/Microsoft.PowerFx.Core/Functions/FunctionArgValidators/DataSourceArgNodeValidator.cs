// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AppMagic.Authoring.Publish;
using Microsoft.PowerFx.Core.Binding;
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
            
            if (ClosestDescendantDataSourceNameVisitor.TryGetDataSourceFirstNameNodes(argNode, binding, out var nodes))
            {
                dsNodes = nodes.ToList();
                return true;
            }

            dsNodes = new List<FirstNameNode>();
            return false;
        }
    }
}
