// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class NotifyFunction : ReflectionFunction
    {
        public NotifyFunction()
        {
            this.ConfigType = typeof(IReplOutput);
        }

        public async Task<BooleanValue> Execute(IReplOutput output, StringValue message, CancellationToken cancel)
        {
            await output.WriteLineAsync(message.Value, OutputKind.Notify, cancel)
                .ConfigureAwait(false);

            // $$$ fix return value ... void / blank?
            return FormulaValue.New(true);
        }
    }

    internal class HelpFunction : ReflectionFunction
    {
        private readonly PowerFxReplBase _repl;

        public HelpFunction(PowerFxReplBase repl)
            : base()
        {
            _repl = repl;
        }

        private async Task WriteAsync(string msg, CancellationToken cancel)
        {
            await _repl.Output.WriteAsync(msg, OutputKind.Notify, cancel)
                .ConfigureAwait(false);
        }

        public async Task<BooleanValue> Execute(CancellationToken cancel)
        {
            await this.WriteAsync("Available functions:\n", cancel)
                .ConfigureAwait(false);

            var column = 0;

            // $$$ better helper
            IEnumerable<string> original =
                _repl.Engine.SupportedFunctions.FunctionNames
                    .Concat(_repl.MetaFunctions.FunctionNames);

            StringBuilder stringBuilder = new StringBuilder();

            var funcNames = original.ToList();
            funcNames.Sort();
            foreach (var func in funcNames)
            {
                stringBuilder.Append($"  {func,-14}");
                if (++column % 5 == 0)
                {
                    stringBuilder.AppendLine();
                }
            }

            stringBuilder.AppendLine();

            await this.WriteAsync(stringBuilder.ToString(), cancel)
                    .ConfigureAwait(false);

            return FormulaValue.New(true);
        }   
    }
}
