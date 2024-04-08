// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers.Notifications
{
    /// <summary>
    /// Backwards compatible OnDidChange notification handler.
    /// Old sdk used to have a delegate to notify the change in document.
    /// This handler just wraps that delegate and calls it. 
    /// </summary>
    internal sealed class BackwardsCompatibleOnDidChangeNotificationHandler : OnDidChangeLanguageServerNotificationHandler
    {
        private readonly LanguageServer.NotifyDidChange _notifyDidChange;

        public BackwardsCompatibleOnDidChangeNotificationHandler(LanguageServer.NotifyDidChange notifyDidChange)
        {
            _notifyDidChange = notifyDidChange;
        }

        protected override async Task OnDidChange(LanguageServerOperationContext operationContext, DidChangeTextDocumentParams didChangeTextDocumentParams, CancellationToken cancellationToken)
        {
            _notifyDidChange?.Invoke(didChangeTextDocumentParams);
        }
    }
}
