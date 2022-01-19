// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp
{
    public class SignatureInformation
    {
        public string Label { get; set; }

        public string Documentation { get; set; }

        public ParameterInformation[] Parameters { get; set; }
    }
}