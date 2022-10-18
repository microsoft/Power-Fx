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
    /// Helper to compose multiple symbol values into a single one. 
    /// Corresponds to <see cref="ReadOnlySymbolTable.Compose(ReadOnlySymbolTable[])"/>.
    /// </summary>
    internal class ComposedReadOnlySymbolTableValues : ReadOnlySymbolValues
    {
        private readonly IEnumerable<ReadOnlySymbolValues> _tables;

        public ComposedReadOnlySymbolTableValues(IEnumerable<ReadOnlySymbolValues> tables)            
        {
            _tables = tables;
            DebugName = string.Join(",", tables.Select(t => t.DebugName));
        }

        public override object GetService(Type serviceType)
        {
            foreach (var table in _tables)
            {
                var service = table.GetService(serviceType);
                if (service != null)
                {
                    return service;
                }
            }

            return null;
        }

        public override bool TryUpdateValue(string name, FormulaValue newValue)
        {
            foreach (var table in _tables)
            {
                var updated = table.TryUpdateValue(name, newValue);
                if (updated)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool TryGetValue(string name, out FormulaValue value)
        {
            foreach (var table in _tables)
            {
                if (table.TryGetValue(name, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        protected override ReadOnlySymbolTable GetSymbolTableSnapshotWorker()
        {
            var symTables = _tables.Select(table => table.GetSymbolTableSnapshot());

            return ReadOnlySymbolTable.Compose(symTables.ToArray());
        }
    }
}
