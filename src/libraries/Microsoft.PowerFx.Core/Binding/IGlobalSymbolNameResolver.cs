// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding.BindInfo;

namespace Microsoft.PowerFx.Core.Binding
{
    // Allows Name resolvers to support global symbols, used for identifying
    // variables in intellisense
    internal interface IGlobalSymbolNameResolver : INameResolver
    {        
        IEnumerable<KeyValuePair<string, NameLookupInfo>> GlobalSymbols { get; }
    }
}
