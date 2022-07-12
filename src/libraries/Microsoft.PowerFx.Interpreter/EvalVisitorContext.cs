// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Interpreter;

namespace Microsoft.PowerFx
{
    internal struct EvalVisitorContext
    {
        public SymbolContext SymbolContext { get; set; }

        public StackDepthCounter StackDepthCounter { get; set; }

        public EvalVisitorContext(SymbolContext symbolContext, StackDepthCounter stackMarker)
        {
            SymbolContext = symbolContext;
            StackDepthCounter = stackMarker;
        }

        public EvalVisitorContext IncrementStackDepthCounter()
        {
            return new EvalVisitorContext(SymbolContext, StackDepthCounter.Increment());
        }
    }
}
