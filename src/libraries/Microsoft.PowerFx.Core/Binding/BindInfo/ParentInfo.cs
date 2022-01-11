// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

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