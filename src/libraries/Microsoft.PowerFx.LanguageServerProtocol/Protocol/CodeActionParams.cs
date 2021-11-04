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

        /// <summary>
        /// Gets or sets text document object.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets current editor value range object.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Code action context carries additonal information.
        /// </summary>
        public CodeActionContext Context { get; set; }
    }
}
