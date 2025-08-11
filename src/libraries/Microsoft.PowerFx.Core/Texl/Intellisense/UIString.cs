// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Object for the UI display string.
    /// </summary>
    public sealed class UIString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIString"/> class with the specified text and no highlight.
        /// </summary>
        /// <param name="text">The display string for the UI.</param>
        public UIString(string text)
            : this(text, -1, -1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIString"/> class with the specified text and highlight range.
        /// </summary>
        /// <param name="text">The display string for the UI.</param>
        /// <param name="highlightStart">The start index of the highlight in the display string.</param>
        /// <param name="highlightEnd">The end index of the highlight in the display string.</param>
        public UIString(string text, int highlightStart, int highlightEnd)
        {
            Contracts.AssertNonEmpty(text);
            Contracts.Assert(highlightStart >= -1 && highlightStart <= highlightEnd);

            Text = text;
            HighlightStart = highlightStart;
            HighlightEnd = highlightEnd;
        }

        /// <summary>
        /// Gets the string that will be displayed to the user.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the start index of the matching string from the input text in the display string.
        /// </summary>
        public int HighlightStart { get; private set; }

        /// <summary>
        /// Gets the end index of the matching string from the input text in the display string.
        /// </summary>
        public int HighlightEnd { get; private set; }
    }
}
