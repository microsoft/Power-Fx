// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Intellisense;

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
        /// <returns>NLHandler for the given scope.</returns>
        NLHandler GetNLHandler(IPowerFxScope scope);
    }
}
