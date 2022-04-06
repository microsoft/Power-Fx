// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal sealed class EnumStore
    {
        internal IEnumerable<EnumSymbol> EnumSymbols { get; }

        private readonly IEnumerable<Tuple<DName, DName, DType>> _enumsWithTypes;

        private EnumStore(IEnumerable<EnumSymbol> enumSymbols, IEnumerable<Tuple<DName, DName, DType>> enumsWithTypes)
        {
            EnumSymbols = enumSymbols;
            _enumsWithTypes = enumsWithTypes;
        }

        internal static EnumStore Build(IEnumerable<EnumSymbol> enumSymbols, IEnumerable<Tuple<DName, DName, DType>> enumsWithTypes)
        {
            return new EnumStore(enumSymbols, enumsWithTypes);
        }

        internal IEnumerable<Tuple<DName, DName, DType>> Enums()
        {
            return _enumsWithTypes;
        }
    }
}
