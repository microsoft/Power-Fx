// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Enumerate through a chain of symbol tables in depth first order. 
    /// Ignores nulls, walks <see cref="ReadOnlySymbolTable.Parent"/>.
    /// Throws if there are any loops. 
    /// </summary>
    internal class SymbolTableEnumerator : IEnumerable<ReadOnlySymbolTable>
    {
        private readonly ReadOnlySymbolTable[] _tables;

        public SymbolTableEnumerator(params ReadOnlySymbolTable[] tables)
        {
            _tables = tables;
        }

        public IEnumerator<ReadOnlySymbolTable> GetEnumerator()
        {
            return GetEnumeratorCore();
        }

        private IEnumerator<ReadOnlySymbolTable> GetEnumeratorCore()
        {
            var seen = new HashSet<ReadOnlySymbolTable>();

            foreach (var table in _tables)
            {
                var t = table;
                while (t != null)
                {
                    if (!seen.Add(t))
                    {
                        throw new InvalidOperationException($"Loop in symbol tables");
                    }

                    yield return t;
                    t = t.Parent;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorCore();
        }
    }
}
