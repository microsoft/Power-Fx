// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// PsuedoFunctions are special control operations that have a function syntax, but don't actually execute as a regular function.
    /// For example `IR(expr)`. 
    /// </summary>
    public interface IPseudoFunction
    {
        /// <summary>
        /// Execute the psuedo function.
        /// </summary>
        /// <param name="checkResult">a check for the inner expression.</param>
        /// <param name="repl">REPL for providing services like output windows.</param>
        /// <param name="cancel">cancellation token.</param>
        public abstract Task ExecuteAsync(CheckResult checkResult, PowerFxREPL repl, CancellationToken cancel);

        /// <summary>
        /// Name of the psuedo function.
        /// </summary>
        /// <returns></returns>
        public abstract string Name { get; }
    }

    /// <summary>
    /// Print the IR() of an expression. 
    /// </summary>
    public class IRPseudoFunction : IPseudoFunction
    {
        public async Task ExecuteAsync(CheckResult checkResult, PowerFxREPL repl, CancellationToken cancel)
        {
            var irText = checkResult.PrintIR();
            await repl.Output.WriteLineAsync(irText, OutputKind.Repl, cancel)
                .ConfigureAwait(false);            
        }

        public string Name => "IR";        
    }

    /// <summary>
    /// Print the suggestions of an expression. 
    /// </summary>
    public class SuggestionsPseudoFunction : IPseudoFunction
    {
        public async Task ExecuteAsync(CheckResult checkResult, PowerFxREPL repl, CancellationToken cancel)
        {
            var innerExp = checkResult.ApplyParse().Text;

            if (innerExp.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && innerExp.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                innerExp = innerExp.Substring(1, innerExp.Length - 2);
            }

            (var expression2, var cursorPosition) = Decode(innerExp);

            var suggestions = repl.Engine.Suggest(repl.Engine.Check(expression2), cursorPosition);
            var suggestionsText = string.Join(", ", suggestions.Suggestions.Select(s => s.DisplayText.Text));

            await repl.Output.WriteLineAsync(suggestionsText, OutputKind.Repl, cancel)
                .ConfigureAwait(false);
        }

        private static (string, int) Decode(string expression)
        {
            var cursorPosition = expression.Length;
            var cursorMatches = Regex.Matches(expression, @"\|");

            if (cursorMatches.Count > 0)
            {
                cursorPosition = cursorMatches[0].Index;
            }

            expression = expression.Replace("|", string.Empty);

            return (expression, cursorPosition);
        }

        public string Name => "Suggestions";
    }
}
