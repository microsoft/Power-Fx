// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Mutable collection for runtime Values for a <see cref="SymbolTable"/>.
    /// </summary>
    public class SymbolValues : ReadOnlySymbolValues
    {
        // Map of name to values. Lazy created.
        private Dictionary<string, FormulaValue> _symbolValues;

        // Services for runtime functions. Lazy created.
        private Dictionary<Type, object> _services;

        /// <summary>
        /// Enable chaining of lookups. Chaining is handled by calling the base methods. 
        /// </summary>
        public ReadOnlySymbolValues Parent { get; init; }

        // SymbolTable describes what values we have in the config. 
        // By default, compute it based on the previous AddVariable calls. 
        // A base class can override and do this more efficiently. 
        protected override ReadOnlySymbolTable GetSymbolTableSnapshotWorker()
        {
            // This creates a new SymbolTable and copies values over. 
            // But we could have an optimize version that returns a 
            // derived ReadOnlySymbolTable who overrides TryLookup and binds directly to
            // our same dictionary. 

            var table = new SymbolTable
            {
                DebugName = DebugName,
                Parent = Parent?.GetSymbolTableSnapshot()
            };

            if (_symbolValues != null)
            {
                foreach (var kv in _symbolValues)
                {
                    table.AddVariable(kv.Key, kv.Value.Type);
                }
            }

            return table;
        }

        public SymbolValues AddService<T>(T data)
        {
            // this changes the symbols.
            Inc();

            if (_services == null)
            {
                _services = new Dictionary<Type, object>();
            }

            // Can't already exist. 
            _services.Add(typeof(T), data);
            return this;
        }

        public SymbolValues Add(string name, FormulaValue value)
        {
            // this changes the symbols.
            Inc();

            if (_symbolValues == null)
            {
                _symbolValues = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);
            }

            // Can't already exist. 
            _symbolValues.Add(name, value);
            return this;
        }

        public override object GetService(Type serviceType)
        {
            if (_services != null && _services.TryGetValue(serviceType, out var data))
            {
                return data;
            }

            if (Parent != null)
            {
                return Parent.GetService(serviceType);
            }

            return null;
        }

        public override bool UpdateValue(string name, FormulaValue newValue)
        {
            if (_symbolValues != null)
            {
                if (_symbolValues.TryGetValue(name, out var value))
                {
                    _symbolValues[name] = newValue;
                    return true;
                }
            }

            if (Parent != null)
            {
                return Parent.UpdateValue(name, newValue);
            }

            return false;
        }

        public override bool TryGetValue(string name, out FormulaValue value)
        {
            if (_symbolValues != null && _symbolValues.TryGetValue(name, out value))
            {
                return true;
            }

            if (Parent != null)
            {
                return Parent.TryGetValue(name, out value);
            }

            value = null;
            return false;
        }
    }
}
