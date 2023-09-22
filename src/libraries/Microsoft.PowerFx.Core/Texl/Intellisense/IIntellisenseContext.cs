// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    [TransportType(TransportKind.ByValue)]
    internal interface IIntellisenseContext
    {
        /// <summary>
        /// The input string for intellisense.
        /// </summary>
        public string InputText { get; }

        /// <summary>
        /// Cursor position for the intellisense input string.
        /// </summary>
        public int CursorPosition { get; }

        /// <summary>
        /// Expected Return Type of the expression when evaluated successfully.
        /// </summary>
        [TransportDisabled]
        public DType ExpectedExpressionReturnType { get; }
    }
}
