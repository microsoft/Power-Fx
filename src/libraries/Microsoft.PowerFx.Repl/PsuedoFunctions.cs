// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Syntax;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// PsuedoFunctions are special control operations that have a function syntax, but don't actually execute as a regular function.
    /// For example `IR(expr)`. 
    /// </summary>
    public interface IPseudoFunction
    {
        /// <summary>
        /// Execute the psuedo function.
        /// </summary>
        /// <param name="checkResult">a check for the inner expression.</param>
        /// <param name="repl">REPL for providing services like output windows.</param>
        /// <param name="cancel">cancellation token.</param>
        public abstract Task ExecuteAsync(CheckResult checkResult, PowerFxREPL repl, CancellationToken cancel);

        /// <summary>
        /// Name of the psuedo function.
        /// </summary>
        /// <returns></returns>
        public abstract string Name { get; }
    }

    /// <summary>
    /// Print the IR() of an expression. 
    /// </summary>
    public class IRPseudoFunction : IPseudoFunction
    {
        public async Task ExecuteAsync(CheckResult checkResult, PowerFxREPL repl, CancellationToken cancel)
        {
            var irText = checkResult.PrintIR();
            await repl.Output.WriteLineAsync(irText, OutputKind.Repl, cancel)
                .ConfigureAwait(false);            
        }

        public string Name => "IR";        
    }
}
