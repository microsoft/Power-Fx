// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

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

        public IServiceProvider Services { get; init; }

        public DType ExpectedExpressionReturnType { get; }

        public IntellisenseContext(string inputText, int cursorPosition)
            : this(inputText, cursorPosition, null)
        {
        }

        public IntellisenseContext(string inputText, int cursorPosition, FormulaType expectedExpressionReturnType)
        {
            Contracts.CheckValue(inputText, "inputText");
            Contracts.CheckParam(cursorPosition >= 0 && cursorPosition <= inputText.Length, "cursorPosition");

            InputText = inputText;
            CursorPosition = cursorPosition;
            ExpectedExpressionReturnType = expectedExpressionReturnType?._type ?? DType.Unknown;
        }

        public void InsertTextAtCursorPos(string insertedText)
        {
            Contracts.AssertValue(insertedText);

            InputText = InputText.Substring(0, CursorPosition) + insertedText + InputText.Substring(CursorPosition);
            CursorPosition += insertedText.Length;
        }
    }
}
