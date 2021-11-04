// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Workpsace edit object model.
    /// </summary>
    public class WorkspaceEdit
    {
        /// <summary>
        /// ctor
        /// </summary>
        public WorkspaceEdit()
        {
            Changes = new Dictionary<string, TextEdit[]>();
        }

        /// <summary>
        /// Gets or sets suggested changes.
        /// </summary>
        public Dictionary<string, TextEdit[]> Changes { get; set; }
    }
}
