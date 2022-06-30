// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class SignatureInformation
    {
        public string Label { get; set; }

        public string Documentation { get; set; }

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "n/a")]
        public ParameterInformation[] Parameters { get; set; }
    }
}