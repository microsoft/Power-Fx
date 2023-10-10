// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class TexlNodeWithTrivia : SourceWithTrivia
    {
        internal TexlNode Node;

        public TexlNodeWithTrivia(TexlNode node, ITexlSource before, ITexlSource after)
            : base(before, after)
        {
            Node = node;
        }
    }
}
