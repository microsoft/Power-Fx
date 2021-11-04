// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CodeAction
    {
        public CodeAction()
        {
            Diagnostics = new Diagnostic[] { };
        }

        public string Title { get; set; }

        public string Kind { get; set; }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        public Diagnostic[] Diagnostics { get; set; }

        public bool IsPreferred { get; set; }

        public WorkspaceEdit Edit { get; set; }

        public CodeActionCommand Command { get; set; }

        public object Data { get; set; }
    }
}
