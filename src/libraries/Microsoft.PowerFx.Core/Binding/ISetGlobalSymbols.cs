// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.PowerFx.Core.IR.Symbols;

namespace Microsoft.PowerFx.Core.Binding
{
    internal interface ISetGlobalSymbols
    {
        internal void SetGlobalSymbols(ImmutableDictionary<string, GlobalSymbol> globalSymbols = null);
    }
}
