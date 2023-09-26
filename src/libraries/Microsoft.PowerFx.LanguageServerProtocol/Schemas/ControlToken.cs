// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Schemas
{
    internal class ControlToken
    {
        // Control name
        public string Name { get; set; }

        // Representing the range of the control token
        public LinkedList<List<uint>> EncodedTokenIndices { get; set; }
    }
}
