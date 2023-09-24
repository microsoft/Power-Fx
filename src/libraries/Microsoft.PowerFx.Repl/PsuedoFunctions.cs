// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Repl
{
    public interface IPseudoFunction
    {
        public abstract void Execute(CallNode callNode, PowerFxReplBase repl, ReadOnlySymbolTable extraSymbolTable, CancellationToken cancel);

        public abstract string Name();
    }

    public class IRPseudoFunction : IPseudoFunction
    {
        public async void Execute(CallNode callNode, PowerFxReplBase repl, ReadOnlySymbolTable extraSymbolTable, CancellationToken cancel)
        {
            var cr = repl.Engine.Check(callNode.Args.ToString(), options: repl.ParserOptions, symbolTable: extraSymbolTable);
            var irText = cr.PrintIR();
            await repl.Output.WriteLineAsync(irText, OutputKind.Repl, cancel)
                .ConfigureAwait(false);
        }

        public string Name()
        {
            return "IR";
        }
    }
}
