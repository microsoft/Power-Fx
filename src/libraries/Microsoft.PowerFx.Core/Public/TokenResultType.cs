// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Token result type (this matches formula bar token type defined in PowerAppsTheme.ts).
    /// </summary>
    public enum TokenResultType
    {
        Boolean,
        Keyword,
        Function,
        Number,
        Operator,
        HostSymbol,
        Variable
    }
}
