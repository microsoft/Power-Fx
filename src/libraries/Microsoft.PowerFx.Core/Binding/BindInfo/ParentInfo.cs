// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Binding information for "Parent" names.
    /// </summary>
    internal sealed class ParentInfo : ControlKeywordInfo
    {
        public override DName Name => new DName(TexlLexer.KeywordParent);

        public ParentInfo(ParentNode node, DPath path, IExternalControl data)
            : base(node, path, data)
        {
        }
    }
}
