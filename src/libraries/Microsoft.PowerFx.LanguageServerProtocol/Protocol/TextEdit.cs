// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Text edit object model.
    /// </summary>
    public class TextEdit
    {
        /// <summary>
        /// Gets or sets the target range object.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Gets or sets new text to be replace when command executed.
        /// </summary>
        public string NewText { get; set; }
    }
}
