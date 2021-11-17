// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Code action fix range location.
    /// </summary>
    public class CodeActionRange
    {
        /// <summary>
        /// Code fix start line number
        /// </summary>
        public int LineStart { get; set; }

        /// <summary>
        /// Code fix start line charactor position
        /// </summary>
        public int CharPositionStart { get; set; }

        /// <summary>
        /// Code fix end line number
        /// </summary>
        public int LineEnd { get; set; }

        /// <summary>
        /// Code fix end line charactor position
        /// </summary>
        public int CharPositionEnd { get; set; }
    }
}
