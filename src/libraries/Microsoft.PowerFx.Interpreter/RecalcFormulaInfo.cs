// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // Information needed for recalc engine
    // Could represent either a fixed variable or a formula. 
    internal class RecalcFormulaInfo
    {
        private RecalcFormulaInfo() 
        { 
        }
        
        public static RecalcFormulaInfo NewVariable(ISymbolSlot slot, string name, FormulaType type)
        {
            return new RecalcFormulaInfo
            {
                Name = name,
                _type = type,
                Slot = slot
            };
        }

        public static RecalcFormulaInfo NewFormula(ISymbolSlot slot, string name, FormulaType type, HashSet<string> dependsOn, TexlBinding binding, Action<string, FormulaValue> onUpdate)
        {
            return new RecalcFormulaInfo
            {
                Name = name,
                _dependsOn = dependsOn,
                _type = type,
                _binding = binding,
                _onUpdate = onUpdate,
                Slot = slot
            };
        }

        public string Name { get; private set; }

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

        // Value is on Parent's symbol table, accessed via Slot. 
        public ISymbolSlot Slot { get; set; }

        // True iff this is a formula that is recalculated. 
        // Formulas aren't mutable. 
        public bool IsFormula => _binding != null;     
    }
}
