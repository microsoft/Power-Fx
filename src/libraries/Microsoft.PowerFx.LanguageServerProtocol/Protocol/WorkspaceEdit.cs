// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Workpsace edit object model.
    /// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#workspaceEdit.
    /// </summary>
    public class WorkspaceEdit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceEdit"/> class.
        /// ctor.
        /// </summary>
        public WorkspaceEdit()
        {
            Changes = new Dictionary<string, TextEdit[]>();
        }

        /// <summary>
        /// Gets or sets suggested changes.
        /// </summary>
        [SuppressMessage("Usage", "CA2227: Collection properties should be read only", Justification = "n/a")]
        public Dictionary<string, TextEdit[]> Changes { get; set; }
    }
}
