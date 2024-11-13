// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl
{
    /// <summary>
    /// This visitor is used to walkthrough the tree to check the existence of a view.
    /// </summary>
    internal sealed class ViewFinderVisitor : IdentityTexlVisitor
    {
        private readonly TexlBinding _txb;

        public bool ContainsView { get; private set; }

        public ViewFinderVisitor(TexlBinding binding)
        {
            Contracts.AssertValue(binding);

            _txb = binding;
        }

        public override void PostVisit(DottedNameNode node)
        {
            var argType = _txb.GetType(node);
            if (argType.Kind == DKind.ViewValue)
            {
                ContainsView = true;
            }
        }

        public override void Visit(FirstNameNode node)
        {
            var info = _txb.GetInfo(node);
            if (info != null && info.Data is IExternalNamedFormula namedRule && namedRule.ContainsReferenceToView)
            {
                ContainsView = true;
            }
        }
    }
}
