// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action request parameters.
    /// </summary>
    public class CodeActionParams
    {
        /// <summary>
        /// ctor
        /// </summary>
        public CodeActionParams()
        {
            Context = new CodeActionContext();
        }

        public TextDocumentIdentifier TextDocument { get; set; }

        public Range Range { get; set; }

        /// <summary>
        /// Code action context carries additonal information.
        /// </summary>
        public CodeActionContext Context { get; set; }
    }
}
