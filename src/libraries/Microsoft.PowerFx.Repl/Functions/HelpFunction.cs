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
    /// Help() function - prints a list of commands. 
    /// </summary>
    internal class HelpFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public HelpFunction(PowerFxREPL repl)
            : base()
        {
            _repl = repl;
        }

        public async Task<BooleanValue> Execute(CancellationToken cancel)
        {
            await _repl.HelpProvider.Execute(_repl, cancel)
                .ConfigureAwait(false);

            return FormulaValue.New(true);
        }
    }

    public class HelpProvider
    {
        public static IEnumerable<string> FunctionsList(PowerFxREPL repl)
        {
            return repl.Engine.SupportedFunctions.FunctionNames
                    .Concat(repl.MetaFunctions.FunctionNames);
        }

        public static string FormatFunctionsList(IEnumerable<string> functionList, int numColumns = 5, int columnWidth = 14)
        {
            var stringBuilder = new StringBuilder();

            var column = 0;

            var funcNames = functionList.ToList();
            funcNames.Sort();
            foreach (var func in funcNames)
            {
                stringBuilder.Append($"  " + func.PadLeft(columnWidth));
                if (++column % numColumns == 0)
                {
                    stringBuilder.AppendLine();
                }
            }

            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

        protected async Task WriteAsync(PowerFxREPL repl, string msg, CancellationToken cancel)
        {
            await repl.Output.WriteAsync(msg, OutputKind.Notify, cancel)
                .ConfigureAwait(false);
        }

        public virtual async Task Execute(PowerFxREPL repl, CancellationToken cancel)
        {
            // $$$ include custom message 
            // $$$ Pointer to web URL?

            await WriteAsync(repl, "Available functions (case sensitive):\n", cancel)
                .ConfigureAwait(false);

            await WriteAsync(repl, FormatFunctionsList(FunctionsList(repl)), cancel)
                    .ConfigureAwait(false);
        }
    }
}
