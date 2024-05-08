// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
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
        private readonly IHostCancelationHandler _cancelationHandler;

        public DefaultLanguageServerOperationHandlerFactory(
            INLHandlerFactory nlHandlerFactory, 
            LanguageServer.NotifyDidChange notifyDidChange,
            IHostCancelationHandler cancelationHandler)
        {
            _nlHandlerFactory = nlHandlerFactory;
            _notifyDidChange = notifyDidChange;
            _cancelationHandler = cancelationHandler;
        }

        public ILanguageServerOperationHandler GetHandler(string lspMethod, HandlerCreationContext creationContext)
        {
            switch (lspMethod)
            {
                case TextDocumentNames.CancelRequest:
                    return new CancelRequestNotificationHandler(_cancelationHandler);
                case CustomProtocolNames.NL2FX:
                    return new Nl2FxLanguageServerOperationHandler(_nlHandlerFactory);
                case CustomProtocolNames.FX2NL:
                    return new Fx2NlLanguageServerOperationHandler(_nlHandlerFactory);
                case CustomProtocolNames.GetCapabilities: 
                    return new GetCustomCapabilitiesLanguageServerOperationHandler(_nlHandlerFactory);
                case TextDocumentNames.Completion:
                    return new CompletionsLanguageServerOperationHandler();
                case TextDocumentNames.SignatureHelp:
                    return new SignatureHelpLanguageServerOperationHandler();
                case TextDocumentNames.RangeDocumentSemanticTokens:
                    return new RangeSemanticTokensLanguageServerOperationHandler();
                case TextDocumentNames.FullDocumentSemanticTokens:
                    return new BaseSemanticTokensLanguageServerOperationHandler();
                case TextDocumentNames.CodeAction:
                    return new CodeActionsLanguageServerOperationHandler(creationContext.onLogUnhandledExceptionHandler);
                case TextDocumentNames.DidChange:
                    return new OnDidChangeLanguageServerNotificationHandler(_notifyDidChange);
                case TextDocumentNames.DidOpen:
                    return new OnDidOpenLanguageServerNotificationHandler();
                case CustomProtocolNames.CommandExecuted:
                    return new CommandExecutedLanguageServerOperationHandler();
                case CustomProtocolNames.InitialFixup:
                    return new InitialFixupLanguageServerOperationHandler();
                default:
                    return null;
            }
        }
    }
}
