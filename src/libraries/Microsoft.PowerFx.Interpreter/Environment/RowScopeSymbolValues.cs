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
    /// Create Symbol values around a RecordValue. 
    /// Ensure lazy evaluation semantics, just like RecordValue/RecordType.
    /// </summary>
    internal class RowScopeSymbolValues : ReadOnlySymbolValues
    {
        private readonly RecordValue _parameter;
        private readonly bool _allowThisRecord;

        public RowScopeSymbolValues(RecordValue parameter, bool allowThisRecord)
        {
            _parameter = parameter;
            _allowThisRecord = allowThisRecord;
        }

        public override bool TryGetValue(string name, out FormulaValue value)
        {
            if (_allowThisRecord && name == "ThisRecord")
            {
                value = _parameter;
                return true;
            }

            if (!_parameter.Type.HasField(name))
            {
                // If it's not in this table. Be sure to not claim it so that 
                // we can still check a parent scope. 
                value = null;
                return false;
            }

            value = _parameter.GetField(name);
            return value != null;
        }

        protected override ReadOnlySymbolTable GetSymbolTableSnapshotWorker()
        {
            SymbolTable table1 = null;
            if (_allowThisRecord)
            {
                table1 = new SymbolTable();
                table1.AddVariable("ThisRecord", _parameter.Type);
            }

            var table = ReadOnlySymbolTable.NewFromRecord(_parameter.Type, table1);
            return table;
        }
    }
}
