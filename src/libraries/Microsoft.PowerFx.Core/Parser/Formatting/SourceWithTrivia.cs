// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Holds sources before and after a token/node.
    /// </summary>
    internal class SourceWithTrivia
    {
        internal SourceList Before { get; }

        internal SourceList After { get; }

        public SourceWithTrivia(SourceList before, SourceList after)
        {
            Before = before;
            After = after;
        }

        public SourceWithTrivia(ITexlSource before = null, ITexlSource after = null)
        {
            Before = before == null ? null : new SourceList(before);
            After = after == null ? null : new SourceList(after);
        }
    }
}
