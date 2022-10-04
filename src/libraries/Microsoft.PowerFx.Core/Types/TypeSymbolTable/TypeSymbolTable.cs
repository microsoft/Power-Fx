// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal abstract class TypeSymbolTable : ReadOnlySymbolTable
    {
        internal abstract bool TryGetTypeName(FormulaType type, out string typeName);

        protected NameLookupInfo ToLookupInfo(FormulaType type)
        {
            return new NameLookupInfo(BindKind.TypeName, type._type, DPath.Root, 0, data: type);
        }
    }
}
