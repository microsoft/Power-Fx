// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    // ParameterInfo may have been a better name, but that conflicts with a type
    // in Microsoft.PowerFx.Core.Functions.TransportSchemas. 
    [DebuggerDisplay("{Name}: {Description}")]
    public class ParameterInfoSignature
    {
        public string Name { get; init; }

        public string Description { get; init; }

        // More fields, like optional? default value?, etc.
    }
}
