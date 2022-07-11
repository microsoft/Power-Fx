// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Interpreter
{
    internal struct EvalVisitorContext
    {
        public SymbolContext SymbolContext;
        public StackMarker StackMarker;

        public EvalVisitorContext(SymbolContext symbolContext, StackMarker stackMarker)
        {
            SymbolContext = symbolContext;
            StackMarker = stackMarker;
        }
    }
}
