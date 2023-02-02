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
#if false
    /// <summary>
    /// Provides symbol table with deferred semantics. 
    /// This is semantically readonly - there is a fixes set of known symbols, just the type information is populated lazily.
    /// Good when we can quickly load the names (to populate the intellisense completion list), but then load remaining symbol details on-demand. 
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    public class DeferredSymbolTable : ReadOnlySymbolTable 
    {
        // $$$ Must be thread safe!!!        
        private readonly object _lock = new object();

        // All possible tables we could add. 
        private readonly DisplayNameProvider _displayNameLookup;

        // Full universe of possible symbols
        // All other symbols are missing. 
        public DeferredSymbolTable(DisplayNameProvider map, Action<string, string> funcAdd)
        {
            _displayNameLookup = map ?? throw new ArgumentNullException(nameof(map));   
        }

        // SymbolTable is conceptually constant. 
        private readonly VersionHash _constant = VersionHash.New();

        internal override VersionHash VersionHash => _constant;

        // Called by 
        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (_displayNameLookup.TryGetLogicalName(name, out var logicalName))
            {
                // This callback will add the symbol, so that when we return, our caller will naturally find it. 
                // But that add will inc the VersionHash, so we override to disable it. 
                _funcAdd(logicalName.Value, name.Value);
            }
            else if (_displayNameLookup.TryGetDisplayName(name, out var displayName))
            {
                _funcAdd(name.Value, displayName.Value);
            }

            /*
             
            var slot = _symbols.AddVariable(logicalName, tableValue.Type, displayName: displayName);
            _tablesLogical2Value[logicalName] = tableValue;
            _parent.SymbolValues.Set(slot, tableValue);
             */
        }        

        // Intellisense completitions 

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            return base.GetTypeFromSlot(slot);
        }        
    }
#endif
}
