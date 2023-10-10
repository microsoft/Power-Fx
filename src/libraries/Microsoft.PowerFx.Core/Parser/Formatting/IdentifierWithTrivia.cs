// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Identifiers in user definitions with comments before and after the identifier.
    /// </summary>
    internal sealed class IdentifierWithTrivia : SourceWithTrivia
    {
        internal string Name;

        public IdentifierWithTrivia(string identifierName, ITexlSource before = null, ITexlSource after = null)
            : base(before, after)
        {
            Name = identifierName;
        }
    }
}
