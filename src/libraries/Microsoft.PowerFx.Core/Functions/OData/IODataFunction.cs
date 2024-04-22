// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.Functions.OData
{
    // Get OData commands during Check 
    internal interface IODataFunction
    {
        // returns null is no delegation is possible
        ODataCommand GetODataCommand(CallNode callNode);
    }
}
