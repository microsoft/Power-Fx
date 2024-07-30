// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging
{
    internal interface ISanitizedNameProvider
    {
        /// <summary>
        /// Attempt to sanitize an identifier using a custom sanitization scheme.
        /// </summary>
        /// <param name="identifier">The identifer.</param>
        /// <param name="sanitizedName">The sanitized name output.</param>
        /// <param name="dottedNameNode">The dotted name node, optional.</param>
        /// <returns>Whether the custom sanitization should be used.</returns>
        bool TrySanitizeIdentifier(Identifier identifier, out string sanitizedName, DottedNameNode dottedNameNode = null);
    }
}
