// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Mutable collection for runtime Values for a <see cref="SymbolTable"/>.
    /// Will lazily create a symbol table. 
    /// To match to an existing symbol table, call <see cref="SymbolExtensions.CreateValues(ReadOnlySymbolTable, ReadOnlySymbolValues[])"/>.
    /// </summary>
    public class SymbolValues : ReadOnlySymbolValues
    {
        // Map of name Slots --> Values. 
        // Index by Slot.SlotIndex. This could be optimized to be a dense array. 
        private readonly Dictionary<int, Tuple<ISymbolSlot, FormulaValue>> _symbolValues = new Dictionary<int, Tuple<ISymbolSlot, FormulaValue>>();

        // Services for runtime functions. Lazy created.
        private Dictionary<Type, object> _services;

        private readonly SymbolTable _symTable;

        /// <summary>
        /// Register an event to invoke when <see cref="Set(ISymbolSlot, FormulaValue)"/> is called.
        /// </summary>
        public event Action<ISymbolSlot, FormulaValue> OnUpdate;

        // Table will be inferred from the values we add. 
        // Take debugName as a parameter so that we can pass it to SymbolTable that we create.
        public SymbolValues(string debugName = null)
            : this(new SymbolTable { DebugName = debugName ?? "RuntimeValues" })
        {
        }

        // Values for an existing table.
        public SymbolValues(SymbolTable table)
            : base(table)
        {
            _symTable = table ?? throw new ArgumentNullException(nameof(table));

            DebugName = table.DebugName;
        }

        public SymbolValues AddService<T>(T data)
        {
            // this changes the symbols.            
            if (_services == null)
            {
                _services = new Dictionary<Type, object>();
            }

            // Can't already exist. 
            _services.Add(typeof(T), data);
            return this;
        }

        /// <summary>
        /// Convenience method to add a new unique symbol.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SymbolValues Add(string name, FormulaValue value)
        {
            var slot = _symTable.AddVariable(name, value.Type, mutable: true);
            Set(slot, value);

            return this;
        }

        public override object GetService(Type serviceType)
        {
            if (_services != null && _services.TryGetValue(serviceType, out var data))
            {
                return data;
            }

            return null;
        }

        public override void Set(ISymbolSlot slot, FormulaValue value)
        {
            ValidateSlot(slot);

            if (value == null)
            {
                _symbolValues.Remove(slot.SlotIndex);
            }
            else
            {
                _symTable.ValidateAccepts(slot, value.Type);
                _symbolValues[slot.SlotIndex] = Tuple.Create(slot, value);
            }

            OnUpdate?.Invoke(slot, value);
        }

        public override FormulaValue Get(ISymbolSlot slot)
        {
            ValidateSlot(slot);

            if (_symbolValues.TryGetValue(slot.SlotIndex, out var value))
            {
                if (!value.Item1.IsDisposed())
                {
                    return value.Item2;
                }
            }

            // Return a blank, which needs to be typed.
            var type = _symTable.GetTypeFromSlot(slot);
            return FormulaValue.NewBlank(type);
        }
    }
}
