// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Repl
{
    public interface IPsuedoFunction
    {
        public abstract void Execute(CallNode callNode, PowerFxReplBase repl, ReadOnlySymbolTable extraSymbolTable, CancellationToken cancel);

        public abstract string Name();
    }

    public class IRPsuedoFunction : IPsuedoFunction
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
