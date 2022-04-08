// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action context object model.
    /// </summary>
    public class CodeActionContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeActionContext"/> class.
        /// ctor.
        /// </summary>
        public CodeActionContext()
        {
            Diagnostics = new Diagnostic[] { };
            Only = new string[] { };
        }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        public Diagnostic[] Diagnostics { get; set; }

        /// <summary>
        /// List of code action kind string values.
        /// </summary>
        public string[] Only { get; set; }
    }
}
