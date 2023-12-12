// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class SignatureHelp
    {
        public SignatureInformation[] Signatures { get; set; }

        public uint ActiveSignature { get; set; }

        public uint ActiveParameter { get; set; }
    }

    public class MarkdownSignatureHelp
    {
        public MarkdownSignatureInfomation[] Signatures { get; set; }

        public uint ActiveSignature { get; set; }

        public uint ActiveParameter { get; set; }

        public MarkdownSignatureHelp(SignatureHelp signatureHelp)
        {
            Signatures = signatureHelp.Signatures.Select(signature => new MarkdownSignatureInfomation(signature)).ToArray();
            ActiveSignature = signatureHelp.ActiveSignature;
            ActiveParameter = signatureHelp.ActiveParameter;
        }
    }
}
