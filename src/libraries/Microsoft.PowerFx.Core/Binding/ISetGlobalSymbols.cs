// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;

namespace Microsoft.PowerFx.Core.Binding
{
    internal interface ISetGlobalSymbols
    {
        void SetGlobalSymbols(IReadOnlyDictionary<string, IGlobalSymbol> globalSymbols = null);
    }
}
