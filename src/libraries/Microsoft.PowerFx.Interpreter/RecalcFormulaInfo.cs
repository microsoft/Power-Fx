// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    // Information needed for recalc engine
    // Could represent either a fixed variable or a formula. 
    internal class RecalcFormulaInfo
    {
        // Other variables that this formula depends on. 
        public HashSet<string> _dependsOn;

        // Immediate users. If this var changes, then we need to update everybody 
        // in the _usedbySet (and its transitive closure). 
        public HashSet<string> _usedBy = new HashSet<string>();

        // The static type this formula evaluates to. 
        public FormulaType _type;

        // User callback to invoke when this formula changes. 
        public Action<string, FormulaValue> _onUpdate;

        // For recalc, needed for execution 
        public TexlBinding _binding; 

        // Cached value
        public FormulaValue _value;
    }
}
