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
        // When a symbol table variable is created, these attributes can be specified.
        // A host can override these settings as they see fit.
        // 
        // Examples by category:
        //
        //                               CanSet   CanMutate   CanSetMutate
        // ================================================================
        //  Data sources                  false     true         false
        //  Scope variables               false     false        false
        //  Global variables (1)          true      true         false
        //  Low code plugin NewRecord     false     false        true
        //  Low code plugin OldRecord     false     false        false
        //
        // (1) CanSetMutate for global variables is false by default today, but we may change this default in the future.

        // Can this symbol be reassigned with a Set function. For example: Set( a, {b:3} )
        public bool CanSet { get; init; }

        // Can this symbol be passed to Collect, Patch, Remove, etc. for mutation. For example: Collect( t, {b:3} )
        // This only applies to symbols of a tabular type. 
        public bool CanMutate { get; init; }

        // Can this symbol be deep mutated with Set. For example: Set( a.b, 3 )
        public bool CanSetMutate { get; init; }
    }
}
