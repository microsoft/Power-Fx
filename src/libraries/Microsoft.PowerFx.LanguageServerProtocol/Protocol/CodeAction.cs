// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action object model.
    /// </summary>
    public class CodeAction
    {
        /// <summary>
        /// ctor
        /// </summary>
        public CodeAction()
        {
            Diagnostics = new Diagnostic[] { };
        }

        /// <summary>
        /// Get or sets the title of the code action.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the code action kind.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        public Diagnostic[] Diagnostics { get; set; }

        /// <summary>
        /// Code action is preferred or not.
        /// </summary>
        public bool IsPreferred { get; set; }

        /// <summary>
        /// Gets or sets supported code action edits.
        /// </summary>
        public WorkspaceEdit Edit { get; set; }

        /// <summary>
        /// Gets or sets code action command.
        /// </summary>
        public CodeActionCommand Command { get; set; }

        /// <summary>
        /// Gets or sets addtional data.
        /// </summary>
        public object Data { get; set; }
    }
}
