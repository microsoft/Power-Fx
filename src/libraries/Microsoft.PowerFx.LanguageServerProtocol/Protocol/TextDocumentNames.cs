// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public static class TextDocumentNames
    {
        public const string Completion = "textDocument/completion";
        public const string DidChange = "textDocument/didChange";
        public const string DidClose = "textDocument/didClose";
        public const string DidOpen = "textDocument/didOpen";
        public const string PublishDiagnostics = "textDocument/publishDiagnostics";
        public const string SignatureHelp = "textDocument/signatureHelp";
        public const string CodeAction = "textDocument/codeAction";
    }
}
