// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Intellisense
{
    internal interface ISpecialCaseHandler
    {
        /// <summary>
        /// Handles special cases as needed by fixing replacementStartIndex and matchingLength.
        /// Additionally, filters suggestion list if needed.
        /// </summary>
        bool Run(IIntellisenseContext context, IntellisenseData.IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions);
    }
}