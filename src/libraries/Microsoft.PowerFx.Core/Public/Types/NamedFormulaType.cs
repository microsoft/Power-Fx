// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    // Useful for representing fields in an aggregate.  
    public class NamedFormulaType
    {
        internal readonly TypedName _typedName;

        public NamedFormulaType(string name, FormulaType type, DName displayName = default)
        {
            _typedName = new TypedName(type._type, new DName(name));
            DisplayName = displayName;
        }

        internal NamedFormulaType(TypedName typedName, DName displayName = default)
        {
            _typedName = typedName;
            DisplayName = displayName;
        }

        public DName Name => _typedName.Name;
        public DName DisplayName { get; }

        public FormulaType Type => FormulaType.Build(_typedName.Type);
    }
}
