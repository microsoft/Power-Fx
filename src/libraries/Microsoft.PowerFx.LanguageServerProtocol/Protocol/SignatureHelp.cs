// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    using System;
    using SignatureHelpCore = Microsoft.PowerFx.Intellisense.SignatureHelp.SignatureHelp;

    /// <summary>
    /// Signature help represents the signature of something
    /// callable. There can be multiple signatures but only one
    /// active and only one active parameter.
    /// </summary>
    public class SignatureHelp
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureHelp"/> class.
        /// </summary>
        public SignatureHelp() 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureHelp"/> class.
        /// Copy a LSP signature from a Core signature. 
        /// </summary>
        /// <param name="sig"></param>
        public SignatureHelp(SignatureHelpCore sig) 
        {
            if (sig.Signatures != null)
            {
                this.Signatures = Array.ConvertAll(sig.Signatures, x => new SignatureInformation(x));
            }
            
            this.ActiveSignature = sig.ActiveSignature;
            this.ActiveParameter = sig.ActiveParameter;
        }

        /// <summary>
        /// One or more signatures. If no signatures are available the signature help
        /// request should return `null`.
        /// </summary>
        public SignatureInformation[] Signatures { get; set; }

        /// <summary>
        /// The active signature. If omitted or the value lies outside the
        /// range of `signatures` the value defaults to zero or is ignored if
        /// the `SignatureHelp` has no signatures.
        ///
        /// Whenever possible implementors should make an active decision about
        /// the active signature and shouldn't rely on a default value.
        ///
        /// In future version of the protocol this property might become
        /// mandatory to better express this.
        /// </summary>
        public uint ActiveSignature { get; set; }

        /// <summary>
        /// The active parameter of the active signature. If omitted or the value
        /// lies outside the range of `signatures[activeSignature].parameters`
        /// defaults to 0 if the active signature has parameters. If
        /// the active signature has no parameters it is ignored.
        /// In future version of the protocol this property might become
        /// mandatory to better express the active parameter if the
        /// active signature does have any.
        /// </summary>
        public uint ActiveParameter { get; set; }
    }
}
