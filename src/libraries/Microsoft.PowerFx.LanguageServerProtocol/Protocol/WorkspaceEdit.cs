// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class WorkspaceEdit
    {
        public WorkspaceEdit()
        {
            Changes = new Dictionary<string, TextEdit[]>();
        }

        public Dictionary<string, TextEdit[]> Changes { get; set; }
    }
}
