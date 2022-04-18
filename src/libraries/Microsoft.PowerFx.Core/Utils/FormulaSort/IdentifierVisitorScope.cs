// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Utils.FormulaSort
{
    internal sealed class IdentifierVisitorScope
    {
        private readonly Dictionary<string, IdentifierVisitorMode> _idents = new Dictionary<string, IdentifierVisitorMode>();
        private readonly IdentifierVisitorMode _mode;
        private readonly IdentifierVisitorScope _parent;

        public IdentifierVisitorScope(IdentifierVisitorMode mode, IdentifierVisitorScope parent = null)
        {
            _mode = mode;
            _parent = parent;
        }

        public void Add(string id, IdentifierVisitorMode mode)
        {
            _idents.Add(id, mode);
        }

        public IdentifierVisitorMode Get(string id)
        {
            if (_idents.ContainsKey(id))
            {
                return _idents[id];
            }

            if (_parent != null)
            {
                return _parent.Get(id);
            }

            return _mode;
        }
    }
}
