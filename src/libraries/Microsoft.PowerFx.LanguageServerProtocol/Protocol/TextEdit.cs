// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class TextEdit
    {
        public Range Range { get; set; }

        public string NewText { get; set; }
    }
}
