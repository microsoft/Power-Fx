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
    /// <summary>
    /// Notify(msg) - prints directly to output.
    /// </summary>
    internal class NotifyFunction : ReflectionFunction
    {
        public NotifyFunction()
                : base("Notify", FormulaType.Void, new[] { FormulaType.String })
        {
            ConfigType = typeof(IReplOutput);
        }

        public async Task<VoidValue> Execute(IReplOutput output, StringValue message, CancellationToken cancel)
        {
            await output.WriteLineAsync(message.Value, OutputKind.Notify, cancel)
                .ConfigureAwait(false);

            return FormulaValue.NewVoid();
        }
    }
}
