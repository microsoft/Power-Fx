// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action object model.
    /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.languageserver.protocol.codeaction?view=visualstudiosdk-2019.
    /// </summary>
    public class CodeAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeAction"/> class.
        /// ctor.
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
        /// Gets or sets supported code action edits.
        /// </summary>
        public WorkspaceEdit Edit { get; set; }

        /// <summary>
        /// Gets or sets code action command.
        /// </summary>
        public CodeActionCommand Command { get; set; }
    }
}
