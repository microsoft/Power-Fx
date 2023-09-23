// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Repl.Functions
{
    // Notify(msg) - prints directly to output
    internal class NotifyFunction : ReflectionFunction
    {
        public NotifyFunction()
        {
            ConfigType = typeof(IReplOutput);
        }

        public async Task<BooleanValue> Execute(IReplOutput output, StringValue message, CancellationToken cancel)
        {
            await output.WriteLineAsync(message.Value, OutputKind.Notify, cancel)
                .ConfigureAwait(false);

            // $$$ fix return value ... void / blank?
            return FormulaValue.New(true);
        }
    }
}
