// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class Diagnostic
    {
        public Diagnostic()
        {
            Range = new Range();
            Message = string.Empty;
        }

        /// <summary>
        /// The range at which the message applies.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// The diagnostic's message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A diagnostic instance may represent an error, warning, hint, etc., and each may impose different
        /// behavior on an editor.  This member indicates the diagnostic's kind.
        /// </summary>
        public DiagnosticSeverity Severity { get; set; }
    }
}
