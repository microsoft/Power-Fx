// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public EvalVisitorContext(SymbolContext symbolContext, EvalVisitorContext previousContext)
        {
            SymbolContext = symbolContext;
            StackDepthCounter = previousContext.StackDepthCounter;
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
