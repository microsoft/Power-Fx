// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Microsoft.PowerFx
{
    // Callback interface to write to output. 
    // Handles different kinds, which could be mapped to different console colors or output streams. 
    public interface IReplOutput
    {
        Task WriteAsync(string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default);
    }

    public static class IReplOutputExtensions
    {
        public static Task WriteLineAsync(this IReplOutput output, string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default)
        {
            return output.WriteAsync(message + "\n", kind, cancel);
        }
    }

    public enum OutputKind
    {
        Control = 0, // Like the prompt 
        Repl, // regular output, such as results to Notify()
        Notify, // from commands printing
        Warning,
        Error,
    }

    // $$$ Generic writer over a TextWriter?
    public class ConsoleWriter : IReplOutput
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
