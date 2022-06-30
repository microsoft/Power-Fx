// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class PublishDiagnosticsParams
    {
        public PublishDiagnosticsParams()
        {
            Uri = string.Empty;
            Diagnostics = Array.Empty<Diagnostic>();
        }

        /// <summary>
        /// The URI for which diagnostic information is reported.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "n/a")]
        public Diagnostic[] Diagnostics { get; set; }
    }
}
