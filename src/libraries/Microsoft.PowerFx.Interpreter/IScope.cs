// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // Lookup. Called by FirstName node to resolve. 
    internal interface IScope
    {
        // Return null if not found (and then caller can chain to parent)
        FormulaValue Resolve(string name);
    }

    // Scope for Reduce function that combines the current row with the accumulator.
    // The accumulator is stored under the reduce field name (e.g., "ThisReduce" or an As-renamed name).
    // Row fields are accessed directly, and the whole row is returned for empty-name access (ThisRecord).
    internal class ReduceScope : IScope
    {
        private readonly RecordValue _row;
        private readonly string _reduceName;
        private readonly FormulaValue _accumulator;

        public ReduceScope(RecordValue row, string reduceName, FormulaValue accumulator)
        {
            _row = row;
            _reduceName = reduceName;
            _accumulator = accumulator;
        }

        public FormulaValue Resolve(string name)
        {
            if (name == _reduceName)
            {
                return _accumulator;
            }

            // Empty name = whole scope access (ThisRecord)
            if (name == string.Empty)
            {
                return _row;
            }

            // Delegate to the row record for field access
            return _row.GetField(name);
        }
    }

    internal class FormulaValueScope : IScope
    {
        public readonly FormulaValue _context;

        public FormulaValueScope(FormulaValue context)
        {
            _context = context;
        }

        public virtual FormulaValue Resolve(string name)
        {
            if (_context is RecordValue recorValue && name != string.Empty)
            {
                return recorValue.GetField(name);
            }

            return _context;
        }
    }
}
