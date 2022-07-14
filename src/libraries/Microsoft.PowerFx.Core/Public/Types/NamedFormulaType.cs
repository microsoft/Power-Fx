// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    // Useful for representing fields in an aggregate.  
    public class NamedFormulaType
    {
        internal readonly TypedName _typedName;

        public NamedFormulaType(string name, FormulaType type, string displayName = null)
        {
            _typedName = new TypedName(type._type, new DName(name));
            DisplayName = displayName == null ? default : new DName(displayName);
        }

        internal NamedFormulaType(TypedName typedName, string displayName = null)
        {
            _typedName = typedName;
            DisplayName = displayName == null ? default : new DName(displayName);
        }

        public DName Name => _typedName.Name;

        public DName DisplayName { get; }

        public FormulaType Type => FormulaType.Build(_typedName.Type);
    }
}
