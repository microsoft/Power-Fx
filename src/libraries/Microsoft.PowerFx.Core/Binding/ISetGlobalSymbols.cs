// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.PowerFx.Core.IR.Symbols;

namespace Microsoft.PowerFx.Core.Binding
{
    internal interface ISetGlobalSymbols
    {
        abstract void SetGlobalSymbols(IReadOnlyDictionary<string, IGlobalSymbol> globalSymbols = null);
    }
}
