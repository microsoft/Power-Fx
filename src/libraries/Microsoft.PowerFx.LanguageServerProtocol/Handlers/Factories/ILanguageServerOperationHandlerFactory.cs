// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Context for handler creation.
    /// </summary>
    /// <param name="onLogUnhandledExceptionHandler"> Handler for unhandled exceptions.</param>
    internal record HandlerCreationContext(LanguageServer.OnLogUnhandledExceptionHandler onLogUnhandledExceptionHandler);

    /// <summary>
    /// Factory to get the handler for a given method.
    /// </summary>
    internal interface ILanguageServerOperationHandlerFactory
    {
        /// <summary>
        /// Get the handler for the given method.
        /// </summary>
        /// <param name="lspMethod"> Lsp Method Identifer.</param>
        /// <param name="creationContext"> Handler Creation Context.</param>
        /// <returns>Handler for the given method.</returns>
        ILanguageServerOperationHandler GetHandler(string lspMethod, HandlerCreationContext creationContext);
    }
}
