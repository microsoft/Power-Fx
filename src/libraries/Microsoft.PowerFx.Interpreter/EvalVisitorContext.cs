// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Interpreter;

namespace Microsoft.PowerFx
{
    internal struct EvalVisitorContext
    {
        public SymbolContext SymbolContext { get; }

        public StackDepthCounter StackDepthCounter { get; }

        //public ReadOnlySymbolValues Symbols { get; }

        public EvalVisitorContext(SymbolContext symbolContext, StackDepthCounter stackMarker)//, ReadOnlySymbolValues symbols)
        {
            SymbolContext = symbolContext;
            StackDepthCounter = stackMarker;
            //Symbols = symbols;
        }

        public EvalVisitorContext(SymbolContext symbolContext, EvalVisitorContext context)
        {
            SymbolContext = symbolContext;
            StackDepthCounter = context.StackDepthCounter;
            //Symbols = context.Symbols;
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
