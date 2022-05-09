// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Intellisense
{
    internal class IntellisenseContext : IIntellisenseContext
    {
        /// <summary>
        /// The input string for intellisense.
        /// </summary>
        public string InputText { get; private set; }

        /// <summary>
        /// Cursor position for the intellisense input string.
        /// </summary>
        public int CursorPosition { get; private set; }

        /// <summary>
        /// Flags controlling Intellisense bahavior.
        /// </summary>
        public IntellisenseFlags Flags { get; private set; }

        public IntellisenseContext(string inputText, int cursorPosition, IntellisenseFlags flags)
        {
            Contracts.CheckValue(inputText, "inputText");
            Contracts.CheckParam(cursorPosition >= 0 && cursorPosition <= inputText.Length, "cursorPosition");

            InputText = inputText;
            CursorPosition = cursorPosition;
            Flags = flags;
        }

        public void InsertTextAtCursorPos(string insertedText)
        {
            Contracts.AssertValue(insertedText);

            InputText = InputText.Substring(0, CursorPosition) + insertedText + InputText.Substring(CursorPosition);
            CursorPosition += insertedText.Length;
        }
    }
}
