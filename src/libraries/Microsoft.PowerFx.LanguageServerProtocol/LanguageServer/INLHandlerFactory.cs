// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Represents a factory for creating NLHandler.
    /// </summary>
    public interface INLHandlerFactory
    {
        /// <summary>
        /// Get the NLHandler from the given scope.
        /// </summary>
        /// <param name="scope">IPowerFxScope instance.</param>
        /// <param name="nlParams">NL operation params.</param>
        /// <returns>NLHandler for the given scope.</returns>
        NLHandler GetNLHandler(IPowerFxScope scope, BaseNLParams nlParams);
    }
}
