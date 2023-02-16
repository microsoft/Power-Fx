// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
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
    /// <summary>
    /// Provides symbol table with deferred semantics. 
    /// This is semantically readonly - there is a fixes set of known symbols, just the type information is populated lazily.
    /// Good when we can quickly load the names (to populate the intellisense completion list), but then load remaining symbol details on-demand. 
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    internal class DeferredSymbolTable : ReadOnlySymbolTable, IGlobalSymbolNameResolver, INameResolver
    {
        // All possible tables we could add. 
        private readonly DisplayNameProvider _displayNameLookup;

        // (Logical,Display) --> return type. 
        // Call back to host to a) lazily get the type, b) notify the host this has been loaded. 
        // Assume it can be invoked multiple times; host must protect
        private readonly Func<string, string, FormulaType> _fetchTypeInfo;

        // Full universe of possible symbols
        // All other symbols are missing. 
        public DeferredSymbolTable(
            DisplayNameProvider map, 
            Func<string, string, FormulaType> fetchTypeInfo)
        {
            _displayNameLookup = map ?? throw new ArgumentNullException(nameof(map));
            _fetchTypeInfo = fetchTypeInfo ?? throw new ArgumentNullException(nameof(fetchTypeInfo));
        }

        // SymbolTable is conceptually constant. 
        private readonly VersionHash _constant = VersionHash.New();

        internal override VersionHash VersionHash => _constant;

        // Must be thread safe!!!        
        // We can have multiple threads reading; which means they may be populating the cache. 
        private readonly object _lock = new object();

        //private readonly Dictionary<string, NameLookupInfo> _variables = new Dictionary<string, NameLookupInfo>();

        private readonly SlotMap<NameLookupInfo?> _slots = new SlotMap<NameLookupInfo?>();

        // Override lookup.
        bool INameResolver.Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences)
        {
            if (_displayNameLookup.TryGetLogicalOrDisplayName(name, out var logical, out var display))
            {
                lock (_lock)
                {
                    // Previously cached 
                    if (_variables.TryGetValue(logical.Value, out nameInfo))
                    {
                        return true;
                    }
                }

                // Callback to host - do not hold lock while invoking this. 
                // Host may do arbitrary operations, including network calls to fetch metadata.
                var type = _fetchTypeInfo(logical.Value, display.Value);

                lock (_lock)
                {
                    nameInfo = AddUnderLock(logical, display, type);
                }

                return true;
            }

            nameInfo = default;
            return false;
        }

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            if (_slots.TryGet(slot.SlotIndex, out var nameInfo))
            {
                return FormulaType.Build(nameInfo.Value.Type);
            }

            throw NewBadSlotException(slot);
        }

        private NameLookupInfo AddUnderLock(DName logical, DName display, FormulaType type)
        {
            var oldSize = _variables.Count;

            // recheck if another thread added while acquired lock. 
            if (_variables.TryGetValue(logical.Value, out var nameInfo))
            {
                return nameInfo;
            }
            
            // This is crticial that we're under lock.
            // We only want to add these once. 
            var slotIndex = _slots.Alloc();

            var data = new NameSymbol(logical, mutable: false)
            {
                Owner = this,
                SlotIndex = slotIndex
            };

            nameInfo = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data: data,
                displayName: display);

            _slots.SetInitial(slotIndex, nameInfo);

            // fails if already added. Shouldn't happen since we're under lock. 
            _variables.Add(logical.Value, nameInfo);
            Contracts.Assert(_variables.Count == oldSize + 1);

            return nameInfo;
        }

        // Intellisense completitions - show types even that we don't have loaded yet. 
        // Intellisense doesn't need the Dtype. 
        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols
        {
            get
            {
                foreach (var kv in _displayNameLookup.LogicalToDisplayPairs)
                {
                    var logical = kv.Key;

                    if (!_variables.TryGetValue(logical.Value, out var nameInfo))
                    {
                        // We don't have type yet, make a placeholder. 
                        var placeholder = Core.Types.DType.ObjNull;
                        var display = kv.Value;

                        nameInfo = new NameLookupInfo(
                            BindKind.PowerFxResolvedObject,
                            placeholder,
                            DPath.Root,
                            0,
                            data: null,
                            displayName: new DName(display));
                    }

                    yield return new KeyValuePair<string, NameLookupInfo>(logical, nameInfo);
                }
            }
        }     
    }
}
