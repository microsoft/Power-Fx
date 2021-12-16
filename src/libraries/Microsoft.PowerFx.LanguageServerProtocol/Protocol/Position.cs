// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class Position
    {
        /// <summary>
        /// Line position in a document (zero-based).
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Character offset on a line in a document (zero-based). Assuming that
        /// the line is represented as a string, the `character` value represents
        /// the gap between the `character` and `character + 1`.
        ///
        /// If the character value is greater than the line length it defaults back
        /// to the line length.
        /// </summary>
        public int Character { get; set; }
    }
}
