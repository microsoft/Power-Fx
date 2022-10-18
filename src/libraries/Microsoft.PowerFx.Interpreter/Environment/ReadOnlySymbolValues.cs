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
    [DebuggerDisplay("{DebugName}")]
    public abstract class ReadOnlySymbolValues : IServiceProvider
    {
        internal VersionHash _version = VersionHash.New();

        private ReadOnlySymbolTable _symbolTableSnapshot;

        // Helper in debugging. Useful when we have multiple symbol tables chained. 
        public string DebugName { get; init; } = "(RuntimeValues)";

        // This will also mark dirty any symbol table that we handed out. 
        public void Inc()
        {
            if (_symbolTableSnapshot != null)
            {
                _symbolTableSnapshot.Inc();
            }

            _version.Inc();
        }

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

        /// <summary>
        /// Get a snapshot of the current set of symbols. 
        /// </summary>
        /// <returns></returns>
        public ReadOnlySymbolTable GetSymbolTableSnapshot()
        {
            if (_symbolTableSnapshot != null)
            {
                // Invalidate the previous one
                _symbolTableSnapshot.Inc();
            }

            _symbolTableSnapshot = GetSymbolTableSnapshotWorker();
            return _symbolTableSnapshot;
        }

        protected abstract ReadOnlySymbolTable GetSymbolTableSnapshotWorker();

        public virtual bool TryGetValue(string name, out FormulaValue value)
        {
            value = null;
            return false;
        }

        // Must be existing symbol
        // Must have same type as before. 
        // Return true on successful update, false if not.
        public virtual bool TryUpdateValue(string name, FormulaValue newValue)
        {
            // Default is to fail - should have been caught by binder?
            return false;
        }

        /// <summary>
        /// Get symbol values where each symbol is a field of the record. 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ReadOnlySymbolValues NewFromRecord(RecordValue parameters)
        {
            return new RowScopeSymbolValues(parameters, false);
        }

        /// <summary>
        /// Includes 'ThisRecord'.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="parent"></param>
        /// <param name="debugName"></param>
        /// <returns></returns>
        public static ReadOnlySymbolValues NewRowScope(RecordValue parameters, ReadOnlySymbolValues parent = null, string debugName = null)
        {
            ReadOnlySymbolValues s = new RowScopeSymbolValues(parameters, true)
            {
                DebugName = debugName ?? "(rowScope)"
            };

            if (parent != null)
            {
                s = Compose(s, parent);
            }

            return s;
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
            var s = new SymbolValues
            {
                Parent = parent
            };
            
            foreach (var kv in values)
            {
                s.Add(kv.Key, kv.Value);
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
            return new ComposedReadOnlySymbolTableValues(tables);
        }
    }
}
