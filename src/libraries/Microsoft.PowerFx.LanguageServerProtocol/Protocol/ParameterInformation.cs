// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Represents a parameter of a callable-signature. A parameter can
    /// have a label and a doc-comment.
    /// </summary>
    public class ParameterInformation
    {
        /// <summary>
        /// The label of this parameter information.
        ///
        /// Either a string or an inclusive start and exclusive end offsets within
        /// its containing signature label. (see SignatureInformation.label). The
        /// offsets are based on a UTF-16 string representation as `Position` and
        /// `Range` does.
        ///
        /// *Note*: a label of type string should be a substring of its containing
        /// signature label. Its intended use case is to highlight the parameter
        /// label part in the `SignatureInformation.label`.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The human-readable doc-comment of this parameter. Will be shown
        /// in the UI but can be omitted.
        /// </summary>
        public string Documentation { get; set; }
    }
}
