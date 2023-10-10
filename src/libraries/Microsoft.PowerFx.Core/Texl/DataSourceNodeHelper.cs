// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl
{
    internal class DataSourceNodeHelper
    {
        internal static bool TryGetDataSourceNodes(CallNode callNode, TexlFunction function, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            var args = callNode.Args.Children.VerifyValue();
            if (function.TryGetDataSourceArgumentIndices(args.Count, out var dataSourceArgumentIndices))
            {
                dsNodes = dataSourceArgumentIndices
                    .Select(args.ElementAtOrDefault)
                    .SelectMany(nodeArg =>
                        ArgValidators.DataSourceArgNodeValidator.TryGetValidValue(nodeArg, binding, out var tmpDsNodes) ? tmpDsNodes : Enumerable.Empty<FirstNameNode>())
                    .Where(dataSourceNode => dataSourceNode is not null)
                    .ToList();
            }
            else
            {
                dsNodes = new List<FirstNameNode>();
            }

            return dsNodes.Any();
        }
    }
}
