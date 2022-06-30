// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

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
            Diagnostics = Array.Empty<Diagnostic>();
            Only = Array.Empty<string>();
        }

        /// <summary>
        /// An array of diagnostic information items.
        /// </summary>
        public IEnumerable<Diagnostic> Diagnostics { get; set; }

        /// <summary>
        /// List of code action kind string values.
        /// </summary>
        public IEnumerable<string> Only { get; set; }
    }
}
