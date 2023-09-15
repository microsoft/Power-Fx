// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    internal class DocumentFormattingParams : LanguageServerRequestBaseParams
    {
        public FormattingOptions Options { get; set; }
    }
}
