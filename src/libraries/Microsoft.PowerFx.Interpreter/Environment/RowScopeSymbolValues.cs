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

        private readonly SymbolTableOverRecordType _table;

        public RowScopeSymbolValues(SymbolTableOverRecordType symTable, RecordValue parameter)
            : base(symTable)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (symTable.Parent != null)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new InvalidOperationException($"Can't handle Record symbols with a parent. Use Compose instead");
            }

            _parameter = parameter;

            DebugName = symTable.DebugName;

            _table = symTable;
        }

        public override void Set(ISymbolSlot slot, FormulaValue value)
        {
            ValidateSlot(slot);

            if (_table.IsThisRecord(slot))
            {
                // Binder should have prevented this since ThisRecord isn't mutable
                throw new InterpreterConfigException($"Can't set ThisRecord");
            }

            _table.SetValue(slot, _parameter, value);
        }

        public override FormulaValue Get(ISymbolSlot slot)
        {
            ValidateSlot(slot);
            return _table.GetValue(slot, _parameter);
        }
    }
}
