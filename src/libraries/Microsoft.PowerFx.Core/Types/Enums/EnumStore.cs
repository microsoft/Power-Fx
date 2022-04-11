// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal sealed class EnumStore : IEnumStore
    {
        public IEnumerable<EnumSymbol> EnumSymbols { get; }

        private EnumStore(IEnumerable<EnumSymbol> enumSymbols)
        {
            EnumSymbols = enumSymbols;
        }

        internal static EnumStore Build(IEnumerable<EnumSymbol> enumSymbols)
        {
            return new EnumStore(enumSymbols);
        }
    }
}
