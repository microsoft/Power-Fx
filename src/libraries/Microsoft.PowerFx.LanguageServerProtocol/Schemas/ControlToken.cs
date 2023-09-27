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

        // Representing the ranges of the control token.
        // Eg: [0,0,0,6] >> start line 0, start character 0, end line 0, end character 6.
        // [0,0,0,6,1,1,1,7] >> First control range start line 0, start character 0, end line 0, end character 6
        // Second control range start line 1, start character 1, end line 1, end character 7.
        public List<uint[]> Ranges { get; set; }
    }
}
