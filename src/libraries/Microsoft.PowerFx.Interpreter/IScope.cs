// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    // Lookup. Called by FirstName node to resolve. 
    internal interface IScope
    {
        // Return null if not found (and then caller can chain to parent)
        FormulaValue Resolve(string name);
    }

    internal class RecordScope : IScope
    {
        public readonly RecordValue _context;
        public RecordScope(RecordValue context)
        {
            _context = context;
        }

        public virtual FormulaValue Resolve(string name)
        {
            // Derived class can have more optimized lookup.

            // $$$ avoid throwing (so we can chain). NEed a "TryLookup"
            foreach (var field in _context.Fields)
            {
                if (name == field.Name)
                {
                    return field.Value;
                }
            }

            return null;
        }
    }
}