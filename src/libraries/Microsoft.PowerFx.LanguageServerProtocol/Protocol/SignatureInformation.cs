// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Represents the signature of something callable. A signature
    /// can have a label, like a function-name, a doc-comment, and
    /// a set of parameters.
    /// </summary>
    public class SignatureInformation
    {
        /// <summary>
        /// The label of this signature. Will be shown in
        /// the UI.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The human-readable doc-comment of this signature. Will be shown
        /// in the UI but can be omitted.
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// The parameters of this signature.
        /// </summary>
        public ParameterInformation[] Parameters { get; set; }

        /// <summary>
        /// The index of the active parameter.
        ///
        /// If provided, this is used in place of `SignatureHelp.activeParameter`.
        /// </summary>
        public uint ActiveParameter { get; set; }
    }
}
