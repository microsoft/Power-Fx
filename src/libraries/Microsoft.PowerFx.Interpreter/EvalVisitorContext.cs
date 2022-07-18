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
        public SymbolContext SymbolContext { get; }

        public StackDepthCounter StackDepthCounter { get; }

        public EvalVisitorContext(SymbolContext symbolContext, StackDepthCounter stackMarker)
        {
            SymbolContext = symbolContext;
            StackDepthCounter = stackMarker;
        }

        public EvalVisitorContext IncrementStackDepthCounter()
        {
            return new EvalVisitorContext(SymbolContext, StackDepthCounter.Increment());
        }
        
        public EvalVisitorContext IncrementStackDepthCounter(SymbolContext symbolContext)
        {
            return new EvalVisitorContext(symbolContext, StackDepthCounter.Increment());
        }
    }
}
