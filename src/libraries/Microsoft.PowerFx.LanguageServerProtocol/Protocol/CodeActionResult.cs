// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

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
    }
}
