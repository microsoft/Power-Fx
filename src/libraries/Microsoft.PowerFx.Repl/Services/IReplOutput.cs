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
    /// Kinds of output from repl. 
    /// </summary>
    public enum OutputKind
    {
        Control = 0, // Like the prompt 
        Repl, // regular output, such as results to Notify()
        Notify, // from commands printing
        Warning, // warning 
        Error, // error message, such as parse error or runtime error. 
    }

    /// <summary>
    /// Callback interface to write to output. 
    /// Handles different kinds, which could be mapped to different console colors or output streams. 
    /// </summary>
    public interface IReplOutput
    {
        /// <summary>
        /// Write the message to the output in the designated kind. 
        /// Does not automatically include a newline. 
        /// It's expected that all kinds of output will be written to the same stream, but just formatted / colorized differently.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="kind"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task WriteAsync(string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default);
    }

    public static class IReplOutputExtensions
    {
        public static Task WriteLineAsync(this IReplOutput output, string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default)
        {
            return output.WriteAsync(message + "\n", kind, cancel);
        }
    }
}
