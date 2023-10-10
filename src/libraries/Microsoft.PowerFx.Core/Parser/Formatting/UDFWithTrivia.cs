// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// User defined function and all the trivia in it, this is solely used for formatting.
    /// </summary>
    internal class UDFWithTrivia
    {
        internal IdentifierWithTrivia Ident { get; }

        internal IdentifierWithTrivia ReturnType { get; }

        internal SourceWithTrivia ReturnTypeColonToken { get; }

        internal TexlNodeWithTrivia Body { get; }

        internal ISet<UDFArgWithTrivia> Args { get; }

        public UDFWithTrivia(IdentifierWithTrivia ident, SourceWithTrivia colonToken, IdentifierWithTrivia returnType, HashSet<UDFArgWithTrivia> args, TexlNodeWithTrivia body)
        {
            Ident = ident;
            ReturnType = returnType;
            Args = args;
            Body = body;
            ReturnTypeColonToken = colonToken;
        }
    }
}
