﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class ComposedReadOnlySymbolValues : ReadOnlySymbolValues
    {
        // Map to composed tables 
        private readonly IReadOnlyDictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> _map;

        private ComposedReadOnlySymbolValues(
            ReadOnlySymbolTable symbolTable,
            IReadOnlyDictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> map)
            : base(symbolTable)
        {
            _map = map;
            DebugName = symbolTable.DebugName;
        }

        // Create SymbolValues to match the SymbolTable.
        // Maintains a tree that maps all ST to the SV. 
        // Graft in existing nodes (avoids allocating a new one). 
        // Parameters allows specifying an override in the map.
        internal static ReadOnlySymbolValues New(
            bool canCreateNew,
            ReadOnlySymbolTable symbolTable,
            params ReadOnlySymbolValues[] existing)
        {
            existing = existing.Where(x => x != null).ToArray();

            var existingMap = new Dictionary<ReadOnlySymbolTable, ReadOnlySymbolValues>();

            // Graft in existing entries.
            // These take precedence and are all added first. 
            foreach (var symValues in existing)
            {
                if (symValues is ComposedReadOnlySymbolValues composed)
                {
                    foreach (var kv in composed._map)
                    {
                        // quick Integrity checks - these should never fail. 
                        if (!object.ReferenceEquals(kv.Key, kv.Value.SymbolTable))
                        {
                            throw new InvalidOperationException($"Table doesn't match");
                        }

                        if (kv.Value is ComposedReadOnlySymbolValues)
                        {
                            throw new InvalidOperationException($"ComposedSymbolValues should have been flattened ");
                        }
                        
                        Add(existingMap, kv.Value);
                    }
                }
                else
                {
                    Add(existingMap, symValues);
                }                
            }

            // CreateValues will remove from existingMap, so anything left is extra.
            var map = new Dictionary<ReadOnlySymbolTable, ReadOnlySymbolValues>();
            CreateValues(canCreateNew, existingMap, map, symbolTable);

            if (existingMap.Count > 0)
            {
                // There were existing SymbolValues that don't match. 
                var kv = existingMap.First();
                var msg = $"SymbolValues '{kv.Value.DebugName}' matches to symbol table '{kv.Key.DebugName}', which is not part of symbol table '{symbolTable?.DebugName}'.";
                throw new InvalidOperationException(msg);
            }

            // Optimization
            if (map.Count == 1)
            {
                var symValues = map.First().Value;
                return symValues;
            }

            return new ComposedReadOnlySymbolValues(symbolTable, map);
        }

        private static void Add(Dictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> map, ReadOnlySymbolValues symValues)
        {
            var symTable = symValues.SymbolTable;
            try
            {
                map.Add(symTable, symValues);
            }
            catch
            {
                // Give better error.
                if (map.TryGetValue(symTable, out var existingSymValues))
                {
                    throw new InvalidOperationException($"SymbolTable {symTable.DebugName()} already has SymbolValues '{existingSymValues.DebugName}' associated with it. Can't add '{symValues.DebugName}'");
                }
            }
        }

        // Walk the symbolTable tree and for each node, create the corresponding symbol values. 
        private static void CreateValues(
            bool canCreateNew,
            Dictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> existingMap,
            Dictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> map,
            ReadOnlySymbolTable symbolTable)
        {
            if (symbolTable == null)
            {
                return;
            }

            if (map.ContainsKey(symbolTable))
            {
                // Common if this was set by existing SymbolValues.
                return;
            }

            if (existingMap.TryGetValue(symbolTable, out var existingValues))
            {
                existingMap.Remove(symbolTable);
                map.Add(symbolTable, existingValues);
                return;
            }

            if (symbolTable is ComposedReadOnlySymbolTable composed)
            {
                foreach (var inner in composed.SubTables)
                {
                    CreateValues(canCreateNew, existingMap, map, inner);
                }

                return;
            }
            else if (symbolTable is SymbolTableOverRecordType)
            {
                // Skip.
                // These must either be grafted in or set via the parameters 
                // In both cases, it would already be set. 
            }
            else if (symbolTable is SymbolTable symbolTable2)
            {
                if (!canCreateNew)
                {
                    // $$$ Move broader
                    if (symbolTable2.NeedsValues) 
                    {
                        var msg = $"Missing SymbolValues for {symbolTable.DebugName()}";
                        throw new InvalidOperationException(msg);
                    }
                }

                var symValues = new SymbolValues(symbolTable2)
                {
                    DebugName = symbolTable2.DebugName
                };

                map[symbolTable] = symValues;
                return;
            }
            else
            {
                throw new NotImplementedException($"Unhandled symbol table kind: {symbolTable.DebugName} of type {symbolTable.GetType().FullName} ");
            }
        }

        /// <summary>
        /// Set a value created by <see cref="SymbolTable.AddVariable(string, FormulaType, bool, string)"/>.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="value"></param>
        public override void Set(ISymbolSlot slot, FormulaValue value)
        {
            var symValues = Resolve(slot);
            symValues.Set(slot, value);
        }

        public override FormulaValue Get(ISymbolSlot slot)
        {
            var symValues = Resolve(slot);
            var value = symValues.Get(slot);
            return value;
        }

        private ReadOnlySymbolValues Resolve(ISymbolSlot slot)
        {
            slot.ThrowIfDisposed();

            if (!_map.TryGetValue(slot.Owner, out var symValues))
            {
                ValidateSlot(slot); // Will throw detailed error
            }

            return symValues;
        }
    }
}
