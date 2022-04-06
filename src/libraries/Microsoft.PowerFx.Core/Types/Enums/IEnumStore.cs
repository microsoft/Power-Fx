// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal interface IEnumStore
    {
        IEnumerable<EnumSymbol> EnumSymbols { get; }
    }
}
