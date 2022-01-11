// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Binding information for "Self".
    /// </summary>
    internal sealed class SelfInfo : ControlKeywordInfo
    {
        public override DName Name => new DName(TexlLexer.KeywordSelf);

        public SelfInfo(SelfNode node, DPath path, IExternalControl data)
            : base(node, path, data)
        {
        }
    }
}