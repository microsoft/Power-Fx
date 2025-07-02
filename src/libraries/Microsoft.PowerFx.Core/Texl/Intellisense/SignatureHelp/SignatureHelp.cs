// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    /// <summary>
    /// Represents signature help information, including available signatures and the currently active signature and parameter.
    /// </summary>
    public class SignatureHelp
    {
        /// <summary>
        /// Gets or sets the available signatures.
        /// </summary>
        public SignatureInformation[] Signatures { get; set; }

        /// <summary>
        /// Gets or sets the index of the active signature.
        /// </summary>
        public uint ActiveSignature { get; set; }

        /// <summary>
        /// Gets or sets the index of the active parameter in the active signature.
        /// </summary>
        public uint ActiveParameter { get; set; }
    }
}
