// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    // Object for the UI display string
    public sealed class UIString
    {
        public UIString(string text)
            : this(text, -1, -1)
        {
        }

        public UIString(string text, int highlightStart, int highlightEnd)
        {
            Contracts.AssertNonEmpty(text);
            Contracts.Assert(highlightStart >= -1 && highlightStart <= highlightEnd);

            Text = text;
            HighlightStart = highlightStart;
            HighlightEnd = highlightEnd;
        }

        /// <summary>
        /// This is the string that will be displayed to the user.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The start index of the matching string from the input text in the display string.
        /// </summary>
        public int HighlightStart { get; private set; }

        /// <summary>
        /// The end Index of the matching string from the input text in the display string.
        /// </summary>
        public int HighlightEnd { get; private set; }
    }
}
