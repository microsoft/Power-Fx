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

        public NamedFormulaType(string name, FormulaType type)
        {
            _typedName = new TypedName(type._type, new DName(name));
        }

        internal NamedFormulaType(TypedName typedName)
        {
            _typedName = typedName;
        }

        public string Name => _typedName.Name;

        public FormulaType Type => FormulaType.Build(_typedName.Type);
    }
}
