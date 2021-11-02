// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Symbols
{
    internal class ScopeSymbol : IScopeSymbol
    {
        public int Id { get; }
        public IReadOnlyList<DName> AccessedFields => _fields;
            
        private List<DName> _fields = new List<DName>();

        public ScopeSymbol(int id)
        {
            Id = id;
        }

        public int AddOrGetIndexForField(DName fieldName)
        {
            if (AccessedFields.Contains(fieldName))
                return _fields.IndexOf(fieldName);

            _fields.Add(fieldName);
            return AccessedFields.Count - 1;
        }

        public override string ToString()
        {
            return $"Scope {Id}";
        }
    }
}
