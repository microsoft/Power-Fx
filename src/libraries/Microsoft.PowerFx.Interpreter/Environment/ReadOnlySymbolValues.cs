// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Runtime values corresponding to static values described in a <see cref="SymbolTable"/>.
    /// See <see cref="SymbolValues"/> for a mutable derived class. 
    /// </summary>
    [DebuggerDisplay("{this.GetType().Name}({DebugName})")]
    public abstract class ReadOnlySymbolValues : IServiceProvider
    {
        private readonly ReadOnlySymbolTable _symbolTable;

        // Helper in debugging. Useful when we have multiple symbol tables chained. 
        public string DebugName { get; init; } = "RuntimeValues";

        /// <summary>
        /// Get the symbol table that these values correspond to.
        /// </summary>
        public ReadOnlySymbolTable SymbolTable => _symbolTable;

        /// <summary>
        /// Expose services to functions. For example, this can provide TimeZone information to the 
        /// Now()/TimeZoneInfo()/etc. Or current culture to the various text parsing functions. 
        /// </summary>
        /// <param name="serviceType">type of service requested.</param>
        /// <returns>Return null if service is not provided.</returns>
        public virtual object GetService(Type serviceType)
        {
            return null;
        }

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        protected ReadOnlySymbolValues(ReadOnlySymbolTable symbolTable)
        {
            _symbolTable = symbolTable;
        }

        internal virtual void AddSymbolMap(IDictionary<ReadOnlySymbolTable, ReadOnlySymbolValues> map)
        {
            // If a derived class supports multiple symbols tables, then override this to add them. 
            map[_symbolTable] = this;
        }

        /// <summary>
        /// Get value of a slot previously provided by <see cref="Set(ISymbolSlot, FormulaValue)"/>. 
        /// </summary>
        /// <param name="slot">Slot provided by the associated SymbolTable. </param>
        /// <returns>Value for this slot or BlankValue if no value is set yet.</returns>
        public abstract FormulaValue Get(ISymbolSlot slot);

        /// <summary>
        /// Set a value for a given slot. 
        /// Set(x, value) function from the language will eventually call this, where the binder 
        /// has resolved 'x' to a slot.
        /// </summary>
        /// <param name="slot">Slot provided by the associated SymbolTable. </param>
        /// <param name="value">new value to update this record to. </param>
        public abstract void Set(ISymbolSlot slot, FormulaValue value);

        /// <summary>
        /// Get symbol values where each symbol is a field of the record. 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ReadOnlySymbolValues NewFromRecord(RecordValue parameters)
        {
            var symTable = ReadOnlySymbolTable.NewFromRecord(parameters.Type, allowThisRecord: false, allowMutable: false);

            return NewFromRecord(symTable, parameters);
        }

        // Create and bind against existing symbol table. 
        public static ReadOnlySymbolValues NewFromRecord(ReadOnlySymbolTable table, RecordValue parameters)
        {
            if (table is SymbolTableOverRecordType recordTable)
            {
                var symValue = new RowScopeSymbolValues(recordTable, parameters);
                return symValue;
            } 
            else
            {
                // Should have been created by ReadOnlySymbolTable.NewFromRecord*
                throw new ArgumentException($"Symbol Table must be for Records");
            }
        }

        /// <summary>
        /// Create values over existing collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="parent"></param>
        public static ReadOnlySymbolValues New<T>(IReadOnlyDictionary<string, T> values, ReadOnlySymbolValues parent = null)
            where T : FormulaValue
        {
            // Accept T to allow derived FormulaValue collections. 
            var s = new SymbolValues();
            
            foreach (var kv in values)
            {
                s.Add(kv.Key, kv.Value);
            }

            if (parent != null)
            {
                return Compose(s, parent);
            }

            return s;
        }

        /// <summary>
        /// Compose multiple symbol values into a single one. 
        /// </summary>
        /// <param name="tables">Ordered list of symbol tables.</param>
        /// <returns></returns>
        public static ReadOnlySymbolValues Compose(params ReadOnlySymbolValues[] tables)
        {
            var list = tables.Where(table => table != null).ToArray();
            if (list.Length == 1)
            {
                return list[0];
            }

            var symTables = Array.ConvertAll(list, symValue => symValue.SymbolTable);

            var symTable = ReadOnlySymbolTable.Compose(symTables);

            var x = symTable.CreateValues(tables);

            return x;
        }

        // Helper to call on Get/Set to ensure slot can be used with this value
        protected void ValidateSlot(ISymbolSlot slot)
        {
            _symbolTable.ValidateSlot(slot);
        }
    }
}
