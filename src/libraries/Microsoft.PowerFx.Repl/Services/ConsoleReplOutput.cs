// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Default implementation of Repl output that writes to console.
    /// </summary>
    public class ConsoleReplOutput : IReplOutput
    {
        public Task WriteAsync(string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default)
        {
            Console.ForegroundColor = kind switch
            {
                OutputKind.Control => ConsoleColor.Cyan,
                OutputKind.Repl => ConsoleColor.Gray,
                OutputKind.Error => ConsoleColor.Red,
                OutputKind.Warning => ConsoleColor.Yellow,
                OutputKind.Notify => ConsoleColor.White,
                _ => ConsoleColor.Gray,
            };

            Console.Write(message);

            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
