// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestHandlerFactory : ILanguageServerOperationHandlerFactory
    {
        private readonly Dictionary<string, ILanguageServerOperationHandler> _handlers = new ();

        public TestHandlerFactory()
        {
        }

        public TestHandlerFactory SetHandler(string method, ILanguageServerOperationHandler handler)
        {
            _handlers[method] = handler;
            return this;
        }

        public ILanguageServerOperationHandler GetHandler(string method, HandlerCreationContext creationContext)
        {
            if (_handlers.TryGetValue(method, out var handler))
            {
                return handler;
            }

            switch (method)
            {
                case CustomProtocolNames.NL2FX:
                    return new BaseNl2FxLanguageServerOperationHandler();
                case CustomProtocolNames.FX2NL:
                    return new BaseFx2NlLanguageServerOperationHandler();
                case CustomProtocolNames.GetCapabilities:
                    return new BackwardsCompatibleGetCustomCapabilitiesLanguageServerOperationHandler(null);
                case TextDocumentNames.Completion:
                    return new BaseCompletionsLanguageServerOperationHandler();
                case TextDocumentNames.SignatureHelp:
                    return new BaseSignatureHelpLanguageServerOperationHandler();
                case TextDocumentNames.RangeDocumentSemanticTokens:
                    return new RangeSemanticTokensLanguageServerOperationHandler();
                case TextDocumentNames.FullDocumentSemanticTokens:
                    return new BaseSemanticTokensLanguageServerOperationHandler();
                case TextDocumentNames.CodeAction:
                    return new BaseCodeActionsLanguageServerOperationHandler(creationContext.onLogUnhandledExceptionHandler);
                case TextDocumentNames.DidChange:
                    return new OnDidChangeLanguageServerNotificationHandler();
                case TextDocumentNames.DidOpen:
                    return new OnDidOpenLanguageServerNotificationHandler();
                case CustomProtocolNames.CommandExecuted:
                    return new BaseCommandExecutedLanguageServerOperationHandler();
                case CustomProtocolNames.InitialFixup:
                    return new InitialFixupLanguageServerOperationHandler();
                default:
                    return null;
            }
        }
    }
}
