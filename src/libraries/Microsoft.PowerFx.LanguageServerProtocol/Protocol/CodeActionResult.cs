// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action response (ex. Quick fix).
    /// </summary>
    [DebuggerDisplay("{Title}: {Text}")]
    public class CodeActionResult
    {
        /// <summary>
        /// Gets or sets title to be displayed on code fix suggestion.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets code fix expression text to be applied.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets code fix range.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Gets or sets code action context.
        /// </summary>
        public CodeActionResultContext ActionResultContext { get; set; }
    }

    internal static class CodeActionExtensions
    {
        public static CodeActionResult New(this CodeFixSuggestion suggestion, string handlerName)
        {
            return new CodeActionResult
            {
                Title = suggestion.Title,
                Text = suggestion.SuggestedText,
                ActionResultContext = new CodeActionResultContext
                {
                    ActionIdentifier = suggestion.ActionIdentifier,
                    HandlerName = handlerName
                }
            };
        }
    }
}
