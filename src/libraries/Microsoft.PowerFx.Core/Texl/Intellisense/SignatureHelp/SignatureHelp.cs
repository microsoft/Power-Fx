// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class SignatureHelp
    {
        public SignatureInformation[] Signatures { get; set; }

        public uint ActiveSignature { get; set; }

        public uint ActiveParameter { get; set; }
    }
}