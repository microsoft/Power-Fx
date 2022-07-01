// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    [SuppressMessage("Naming", "CA1724: Type names should not match namespaces", Justification = "n/a")]    
    public class SignatureHelp
    {
        public IEnumerable<SignatureInformation> Signatures { get; set; }

        public uint ActiveSignature { get; set; }

        public uint ActiveParameter { get; set; }
    }
}
