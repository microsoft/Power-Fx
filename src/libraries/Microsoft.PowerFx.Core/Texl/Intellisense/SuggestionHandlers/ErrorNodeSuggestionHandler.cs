// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
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