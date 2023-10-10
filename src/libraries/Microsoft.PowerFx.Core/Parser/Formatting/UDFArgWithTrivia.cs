// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Arguments in user defined functions and their trivia.
    /// </summary>
    internal class UDFArgWithTrivia
    {
        internal IdentifierWithTrivia NameIdent;

        internal IdentifierWithTrivia TypeIdent;

        internal int ArgIndex;

        public UDFArgWithTrivia(IdentifierWithTrivia nameIdent, IdentifierWithTrivia typeIdent, int argIndex)
        {
            NameIdent = nameIdent;
            TypeIdent = typeIdent;
            ArgIndex = argIndex;
        }
    }
}
