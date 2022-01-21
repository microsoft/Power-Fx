// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Provides quick fix.
    /// </summary>
    public interface IPowerFxScopeQuickFix
    {
        /// <summary>
        /// Provider quick fix suggesions.
        /// </summary>
        /// <param name="expression">The formula expression.</param>
        /// <returns>Collection of quick fixes.</returns>
        CodeActionResult[] Suggest(string expression);
    }
}
