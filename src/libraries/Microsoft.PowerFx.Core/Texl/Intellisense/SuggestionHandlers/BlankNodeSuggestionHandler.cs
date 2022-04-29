// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class BlankNodeSuggestionHandler : ErrorNodeSuggestionHandlerBase
        {
            public BlankNodeSuggestionHandler()
                : base(NodeKind.Blank)
            {
            }
        }
    }
}