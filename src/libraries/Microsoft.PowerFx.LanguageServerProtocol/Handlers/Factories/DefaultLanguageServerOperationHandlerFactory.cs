// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Handlers.Notifications;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A default and backwards compatible implementation of <see cref="ILanguageServerOperationHandlerFactory"/>.
    /// </summary>
    internal class DefaultLanguageServerOperationHandlerFactory : ILanguageServerOperationHandlerFactory
    {
        // The following properties are not in the creation context as they exist for backwards compatibility.
        // if all hosts start using the new design, these properties can be removed.
        private readonly INLHandlerFactory _nlHandlerFactory;
        private readonly LanguageServer.NotifyDidChange _notifyDidChange;

        public DefaultLanguageServerOperationHandlerFactory(INLHandlerFactory nlHandlerFactory, LanguageServer.NotifyDidChange notifyDidChange)
        {
            _nlHandlerFactory = nlHandlerFactory;
            _notifyDidChange = notifyDidChange;
        }

        public ILanguageServerOperationHandler GetHandler(string lspMethod, HandlerCreationContext creationContext)
        {
            switch (lspMethod)
            {
                case CustomProtocolNames.NL2FX:
                    return new BackwardsCompatibleNl2FxLanguageServerOperationHandler(_nlHandlerFactory);
                case CustomProtocolNames.FX2NL:
                    return new BackwardsCompatibleFx2NlLanguageServerOperationHandler(_nlHandlerFactory);
                case CustomProtocolNames.GetCapabilities: 
                    return new BackwardsCompatibleGetCustomCapabilitiesLanguageServerOperationHandler(_nlHandlerFactory);
                case TextDocumentNames.Completion:
                    return new BaseCompletionsLanguageServerOperationHandler();
                case TextDocumentNames.SignatureHelp:
                    return new BaseSignatureHelpLanguageServerOperationHandler();
                case TextDocumentNames.RangeDocumentSemanticTokens:
                    return new BaseSemanticTokensLanguageServerOperationHandler(true);
                case TextDocumentNames.FullDocumentSemanticTokens:
                    return new BaseSemanticTokensLanguageServerOperationHandler(false);
                case TextDocumentNames.CodeAction:
                    return new BaseCodeActionsLanguageServerOperationHandler(creationContext.onLogUnhandledExceptionHandler);
                case TextDocumentNames.DidChange:
                    return new BackwardsCompatibleOnDidChangeNotificationHandler(_notifyDidChange);
                case TextDocumentNames.DidOpen:
                    return new OnDidOpenLanguageServerNotificationHandler();
                case CustomProtocolNames.CommandExecuted:
                    return new BaseCommandExecutedLanguageServerOperationHandler();
                case CustomProtocolNames.InitialFixup:
                    return new InitialFixupLanguageServerOperationHandler();
                default:
                    return new NoopLanguageServerOperationHandler();
            }
        }
    }
}
