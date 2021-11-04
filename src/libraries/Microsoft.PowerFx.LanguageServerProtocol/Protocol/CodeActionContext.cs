// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeActionContext
    {
        /// <summary>
        /// ctor
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
        /// List code code action kind string values.
        /// </summary>
        public string[] Only { get; set; }
    }
}
