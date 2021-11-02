// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class PublishDiagnosticsParams
    {
        public PublishDiagnosticsParams()
        {
            Uri = string.Empty;
            Diagnostics = new Diagnostic[] { };
        }

        /// <summary>
        /// The URI for which diagnostic information is reported.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        public Diagnostic[] Diagnostics { get; set; }
    }
}
