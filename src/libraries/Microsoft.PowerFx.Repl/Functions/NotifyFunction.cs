// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Functions
{
    /// <summary>
    /// Notify(msg) - prints directly to output.
    /// </summary>
    internal class NotifyFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public NotifyFunction(PowerFxREPL repl)
        {
            _repl = repl;
        }

        public async Task<BooleanValue> Execute(StringValue message, CancellationToken cancel)
        {
            return await _repl.NotifyProvider.Execute(_repl, message, cancel)
                .ConfigureAwait(false);
        }
    }

    public class NotifyProvider
    {
        public async Task<BooleanValue> Execute(PowerFxREPL repl, StringValue message, CancellationToken cancel)
        {
            await repl.Output.WriteLineAsync(message.Value, OutputKind.Notify, cancel)
                .ConfigureAwait(false);

            return FormulaValue.New(true);
        }
    }
}
