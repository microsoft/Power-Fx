// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// Interface used for TexlFunctions to override the default behavior for intellisense suggestions.
    /// </summary>
    internal interface ISuggestionAwareFunction
    {
        // Returns true if it's valid to suggest ThisItem for this function as an argument.
        bool CanSuggestThisItem { get; }
    }
}
