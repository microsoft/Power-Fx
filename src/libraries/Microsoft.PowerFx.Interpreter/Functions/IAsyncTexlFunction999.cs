// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// Invoker to execute a function via the interpreter. 
    /// </summary>
    public interface IAsyncTexlFunction999 
    {
        // $$$ Obiously, give better name. 

        Task<FormulaValue> InvokeAsync(FunctionInvokeInfo invokeInfo, CancellationToken cancellationToken);
    }

    /// <summary>
    /// The information necessary to invoke a function. 
    /// </summary>
    [ThreadSafeImmutable]
    public class FunctionInvokeInfo
    {
        /// <summary>
        /// The arguments passed to this function. 
        /// </summary>
        public IReadOnlyList<FormulaValue> Args { get; init; }

        // !!! Args can be lambdas that close over _evalVisitor/context.
        // So this is not reusable across invokes...
        // Should we embrace that and put the cancellation token on this?

        // Some functions are polymorphic and need to know the return type.
        // Expected return type computed by the binder. 
        public FormulaType ReturnType => this.IRContext.ResultType;

        // $$$ Or Just put TryGetService directly on here?
        public IServiceProvider FunctionServices { get; init; }

        // Since every method impl would get this, consider add other useful operators on here, like:
        //  - CheckCancel?
        //  - error checks, blank checks. 
#if true
        // Can we find a way to get rid of these ones? Keep internal to limit usage.

        // IrContext has a mutability flag. 
        internal IRContext IRContext { get; set; }

        // Get rid of these... Capture in them in the closure of a lambdaValue.
        internal EvalVisitor Runner { get; init; }

        internal EvalVisitorContext Context { get; init; }
#endif
    }
}
