// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{    
    public class SymbolProperties
    {
        // Can this symbol be reassigned with a Set() function. 
        public bool CanSet { get; init; }

        // Can this symbol be passed to Collect, Patch() for mutation 
        // This only applies to symbols of a tabular type. 
        public bool CanMutate { get; init; }

        // Can this symbol be deep mutated with Set()
        // This only applies to symbols of a tabular type. 
        public bool CanSetMutate { get; init; }

        // Is Copyable?
    }
}
