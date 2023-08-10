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
