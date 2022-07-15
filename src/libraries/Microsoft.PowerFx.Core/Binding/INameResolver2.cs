// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding.BindInfo;

namespace Microsoft.PowerFx.Core.Binding
{
    internal interface INameResolver2 : INameResolver
    {
        IReadOnlyDictionary<string, NameLookupInfo> GlobalSymbols { get; }
    }
}
