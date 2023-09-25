// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
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
        public abstract void Execute(CallNode callNode, PowerFxREPL repl, ReadOnlySymbolTable extraSymbolTable, CancellationToken cancel);

        public abstract string Name();
    }

    /// <summary>
    /// Print the IR() of an expression. 
    /// </summary>
    public class IRPseudoFunction : IPseudoFunction
    {
        public async void Execute(CallNode callNode, PowerFxREPL repl, ReadOnlySymbolTable extraSymbolTable, CancellationToken cancel)
        {
            try
            {
                var cr = repl.Engine.Check(callNode.Args.ToString(), options: repl.ParserOptions, symbolTable: extraSymbolTable);
                var irText = cr.PrintIR();
                await repl.Output.WriteLineAsync(irText, OutputKind.Repl, cancel)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // $$$ cancelled task, or a normal abort? 
                // Signal to caller that we're done. 
                throw;
            }
            catch (Exception e)
            {
                await repl.OnEvalExceptionAsync(e, cancel)
                    .ConfigureAwait(false);
            }
        }

        public string Name()
        {
            return "IR";
        }
    }
}
