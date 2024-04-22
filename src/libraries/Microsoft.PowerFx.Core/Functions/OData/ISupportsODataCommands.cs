// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions.OData
{
    // Used on TabularTableValue-s to cumulate OData command at runtime
    internal interface ISupportsODataCommands
    {
        bool TryAddODataCommand(ODataCommand command);
    }
}
