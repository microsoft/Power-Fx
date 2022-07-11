// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx
{
    internal struct EvalVisitorContext
    {
        public SymbolContext SymbolContext { get; set; }

        public StackMarker StackMarker { get; set; }

        public EvalVisitorContext(SymbolContext symbolContext, StackMarker stackMarker)
        {
            SymbolContext = symbolContext;
            StackMarker = stackMarker;
        }
    }
}
