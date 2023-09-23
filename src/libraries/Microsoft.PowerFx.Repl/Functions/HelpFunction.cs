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
            // $$$ Pointer to web URL?

            await _repl.Output.WriteAsync(msg, OutputKind.Notify, cancel)
                .ConfigureAwait(false);
        }

        public async Task<BooleanValue> Execute(CancellationToken cancel)
        {
            // $$$ include custom message 

            await WriteAsync("Available functions:\n", cancel)
                .ConfigureAwait(false);

            var column = 0;

            // $$$ better helper
            IEnumerable<string> original =
                _repl.Engine.SupportedFunctions.FunctionNames
                    .Concat(_repl.MetaFunctions.FunctionNames);

            var stringBuilder = new StringBuilder();

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

            await WriteAsync(stringBuilder.ToString(), cancel)
                    .ConfigureAwait(false);

            return FormulaValue.New(true);
        }
    }
}
