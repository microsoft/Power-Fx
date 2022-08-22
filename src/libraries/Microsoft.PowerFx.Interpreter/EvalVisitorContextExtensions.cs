// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Interpreter
{
    internal static class EvalVisitorContextExtensions
    {
        public static EvalVisitorContext NewScope(this EvalVisitorContext context, SymbolContext newScope)
        {
            return new EvalVisitorContext(newScope, context);
        }
    }
}
