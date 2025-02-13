﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl
{
    /// <summary>
    /// This visitor is used to walkthrough the first node of a filter to get the datasource name and
    /// whether or not there is any other filter sub expression that uses a view.
    /// </summary>
    internal sealed class ViewFilterDataSourceVisitor : IdentityTexlVisitor
    {
        private const string FilterFunctionName = "Filter";
        private readonly TexlBinding _txb;

        public IExternalCdsDataSource CdsDataSourceInfo { get; private set; }

        public bool ContainsViewFilter { get; private set; }

        public ViewFilterDataSourceVisitor(TexlBinding binding)
        {
            Contracts.AssertValue(binding);

            _txb = binding;
        }

        public override void Visit(FirstNameNode node)
        {
            if (_txb.EntityScope.TryGetDataSource(node, out var info) && info.Kind == DataSourceKind.CdsNative)
            {
                CdsDataSourceInfo = info as IExternalCdsDataSource;
            }
        }

        public override void PostVisit(CallNode node)
        {
            // Check if there is a filter node using view.
            if (node?.Head?.Name.Value == FilterFunctionName)
            {
                foreach (var arg in node.Args.Children)
                {
                    var argType = _txb.GetType(arg);
                    if (argType.Kind == DKind.ViewValue)
                    {
                        ContainsViewFilter = true;
                    }
                }
            }
        }
    }
}
