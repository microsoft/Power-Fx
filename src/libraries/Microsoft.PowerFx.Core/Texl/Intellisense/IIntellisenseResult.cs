// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Intellisense
{
    public interface IIntellisenseResult
    {
        /// <summary>
        /// Enumerates suggestions for the current position in some specified input.
        /// </summary>
        IEnumerable<IIntellisenseSuggestion> Suggestions { get; }

        /// <summary>
        /// Returns the start index of the input string at which the suggestion has to be replaced upon selection of the suggestion.
        /// </summary>
        int ReplacementStartIndex { get; }

        /// <summary>
        /// Returns the length of text to be replaced with the current suggestion.
        /// </summary>
        int ReplacementLength { get; }

        /// <summary>
        /// A boolean value indicating whether the cursor is in function scope or not.
        /// </summary>
        bool IsFunctionScope { get; }

        /// <summary>
        /// Index of the overload in 'FunctionOverloads' to be displayed in the UI.
        /// This is equal to -1 when IsFunctionScope = False.
        /// </summary>
        int CurrentFunctionOverloadIndex { get; }

        /// <summary>
        /// Enumerates function overloads for the function to be displayed.
        /// This is empty when IsFunctionScope = False.
        /// </summary>
        IEnumerable<IIntellisenseSuggestion> FunctionOverloads { get; }

        /// <summary>
        /// Exception information in event of error.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Function signature help for this result, complies to Language Server Protocol.
        /// </summary>
        public SignatureHelp.SignatureHelp SignatureHelp { get; }
    }
}
