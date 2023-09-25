// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using static System.Net.WebRequestMethods;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Functions
{
    /// <summary>
    /// Help() function - prints a list of commands. 
    /// </summary>
    internal class Help0Function : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public Help0Function(PowerFxREPL repl)
                : base("Help", FormulaType.Boolean)
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

    /// <summary>
    /// Help(string) function - prints information on a particular function or topic.
    /// </summary>
    internal class Help1Function : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public Help1Function(PowerFxREPL repl)
                : base("Help", FormulaType.Boolean, new[] { FormulaType.String })
        {
            _repl = repl;
        }

        public async Task<BooleanValue> Execute(StringValue context, CancellationToken cancel)
        {
            await _repl.HelpProvider.Execute(_repl, cancel, context.Value)
                .ConfigureAwait(false);

            return FormulaValue.New(true);
        }
    }

    public class HelpProvider
    {
        public const string FormulaRefURL = "https://aka.ms/Power-Fx-Formula-Reference";

        public static IEnumerable<string> FunctionsList(PowerFxREPL repl)
        {
            return repl.Engine.SupportedFunctions.FunctionNames
                    .Concat(repl.MetaFunctions.FunctionNames);
        }

        public static string FormatFunctionsList(IEnumerable<string> functionList, int numColumns = 5, int columnWidth = 14, int leftPadding = 2)
        {
            var stringBuilder = new StringBuilder();

            var column = 0;

            var funcNames = functionList.ToList();
            funcNames.Sort();
            foreach (var func in funcNames)
            {
                stringBuilder.Append(string.Empty.PadLeft(leftPadding) + func.PadRight(columnWidth));
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

        public virtual async Task Execute(PowerFxREPL repl, CancellationToken cancel, string context = null)
        {
            // default implementation ignores context and returns the full funciton list

            await WriteAsync(repl, "Available functions (case sensitive):\n", cancel)
                .ConfigureAwait(false);

            await WriteAsync(repl, FormatFunctionsList(FunctionsList(repl)), cancel)
                    .ConfigureAwait(false);

            await WriteAsync(repl, $"\nFormula reference: {FormulaRefURL}\n\n", cancel)
                .ConfigureAwait(false);
        }
    }
}
