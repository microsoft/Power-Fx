// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class CompletionResult
    {
        public bool IsIncomplete { get; set; }

        public CompletionItem[] Items { get; set; } = new CompletionItem[] { };
    }
}
