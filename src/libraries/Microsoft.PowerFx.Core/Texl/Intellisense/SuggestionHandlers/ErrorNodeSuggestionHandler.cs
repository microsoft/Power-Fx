// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class ErrorNodeSuggestionHandler : ErrorNodeSuggestionHandlerBase
        {
            public ErrorNodeSuggestionHandler()
                : base(NodeKind.Error)
            {
            }
        }
    }
}
