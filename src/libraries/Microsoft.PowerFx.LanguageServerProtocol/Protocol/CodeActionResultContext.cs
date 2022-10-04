// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// CodeAction context object model.
    /// </summary>
    public class CodeActionResultContext
    {
        public string ProviderName { get; set; }

        public string ActionIdentifier { get; set; }
    }
}
