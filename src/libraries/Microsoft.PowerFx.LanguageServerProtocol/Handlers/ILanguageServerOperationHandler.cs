// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A interface representing a handler for a language server operation.
    /// </summary>
    public interface ILanguageServerOperationHandler
    {
        /// <summary>
        /// The LSP method that this handler is for.
        /// </summary>
        string LspMethod { get; }

        /// <summary>
        /// Asynchronously handles the operation.
        /// This function by design doesn't return value. 
        /// The operation context has a output builder and the handler should use it to write the response.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronously handles the operation.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        void Handle(LanguageServerOperationContext operationContext);
    }
}
