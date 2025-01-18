// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// The parameters to use with <see cref="IFunctionInvoker"/>.
    /// The information necessary to invoke a function via <see cref="RecalcEngine"/>.
    /// The arguments here may be closured over an instance of EvalVisitor/EvalContext, so this 
    /// is only valid for a specific invocation and can't be reused across invokes. 
    /// </summary>
    [ThreadSafeImmutable]
    public class FunctionInvokeInfo
    {
        /// <summary>
        /// The arguments passed to this function. 
        /// These may be closed over context and specific to this invocation, so they should 
        /// not be saved or used outside of this invocation. 
        /// </summary>
        public IReadOnlyList<FormulaValue> Args { get; init; }

        /// <summary>
        /// The expected return type of this function. This is computed by the binder. 
        /// Some functions are polymorphic and the return type depends on the intput argument types. 
        /// </summary>
        public FormulaType ReturnType => this.IRContext.ResultType;
        
        /// <summary>
        /// services for function invocation. This is the set from <see cref="RuntimeConfig.ServiceProvider"/>. 
        /// </summary>
        public IServiceProvider FunctionServices { get; init; }

        // Since every method impl would get this, consider add other useful operators on here, like:
        //  - CheckCancel?
        //  - error checks, blank checks. 

        // Can we find a way to get rid of these ones? Keep internal to limit usage.
        // Keep internal until we kind a way to remove. 
        #region Remove these 

        // IrContext has a mutability flag. 
        internal IRContext IRContext { get; set; }

        // https://github.com/microsoft/Power-Fx/issues/2819
        // Get rid of these... Capture in them in the closure of a lambdaValue.
        internal EvalVisitor Runner { get; init; }

        internal EvalVisitorContext Context { get; init; }
        #endregion 

        // Since this is immutable, clone to get adjusted parameters. 
        public FunctionInvokeInfo CloneWith(IReadOnlyList<FormulaValue> newArgs)
        {
            return new FunctionInvokeInfo
            {
                Args = newArgs,
                FunctionServices = this.FunctionServices,
                IRContext = this.IRContext,
                Runner = this.Runner,
                Context = this.Context
            };
        }

        // Helper to create simple case, primarily for testing. 
        internal static FunctionInvokeInfo New(FormulaType returnType, params FormulaValue[] args)
        {
            return new FunctionInvokeInfo
            {
                Args = args,
                IRContext = IRContext.NotInSource(returnType)
            };
        }
    }
}
