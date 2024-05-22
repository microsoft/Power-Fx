// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class TestHandlerFactory : ILanguageServerOperationHandlerFactory
    {
        private readonly Dictionary<string, ILanguageServerOperationHandler> _handlers = new ();

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
                    return new Nl2FxLanguageServerOperationHandler(null);
                case CustomProtocolNames.FX2NL:
                    return new Fx2NlLanguageServerOperationHandler(null);
                case CustomProtocolNames.GetCapabilities:
                    return new GetCustomCapabilitiesLanguageServerOperationHandler(null);
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
                    return new OnDidChangeLanguageServerNotificationHandler(null);
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
