// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Logging
{
    internal interface ISanitizedNameProvider
    {
        /// <summary>
        /// Attempt to sanitize a first name node using a custom sanitization scheme.
        /// </summary>
        /// <param name="node">The FirstNameNode.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="sanitizedName">The sanitized name output.</param>
        /// <returns>Whether the custom sanitization should be used.</returns>
        bool TrySanitizeFirstNameNode(FirstNameNode node, TexlBinding binding, out string sanitizedName);
    }
}
