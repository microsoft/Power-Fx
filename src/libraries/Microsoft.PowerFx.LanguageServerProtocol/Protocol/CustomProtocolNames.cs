// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Custom LSP protocol starts with '$/', not all client supports.
    /// </summary>
    public static class CustomProtocolNames
    {
        public const string PublishTokens = "$/publishTokens";
        public const string InitialFixup = "$/initialFixup";
        public const string PublishExpressionType = "$/publishExpressionType";
        public const string CommandExecuted = "$/commandExecuted";

        public const string GetCapabilities = "$/getcapabilities";
        public const string NL2FX = "$/nl2fx";
        public const string FX2NL = "$/fx2nl";
    }
}
