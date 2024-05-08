// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Host contains the source of cancellation for LSP requests.
    /// This interface creates a bridge between LSP and host to allow host to cancel LSP requests.
    /// </summary>
    public interface IHostCancelationHandler
    {
        /// <summary>
        /// Allows the host to cancel a request identified by requestId.
        /// </summary>
        /// <param name="requestId">Lsp Request Id.</param>
        void CancelByRequestId(string requestId);
    }
}
