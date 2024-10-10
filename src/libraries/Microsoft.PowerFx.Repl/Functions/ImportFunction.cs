// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Functions
{
    /// <summary>
    /// Import a module.
    /// </summary>
    internal class ImportFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public ImportFunction(PowerFxREPL repl)
            : base("Import", FormulaType.Void, new[] { FormulaType.String })
        {
            ConfigType = typeof(IReplOutput);
            _repl = repl;
        }

        public async Task<VoidValue> Execute(IReplOutput output, StringValue name, CancellationToken cancel)
        {
            var errors = new List<ExpressionError>();

            var ctx = new ModuleLoadContext(_repl.Engine.GetCombinedEngineSymbols());

            string filename = name.Value;
            var module = await ctx.LoadFromFileAsync(filename, errors).ConfigureAwait(false);

            foreach (var error in errors)
            {
                // Adjust to file...? 
                var loc = error.FragmentLocation;

                var shortName = Path.GetFileName(loc.Filename);

                var prefix = error.IsWarning ? "Warning" : "Error";
                var kind = error.IsWarning ? OutputKind.Warning : OutputKind.Error;

                var msg = $"{prefix}: {shortName} ({loc.LineStart},{loc.ColStart}): {error.Message}";

                await output.WriteLineAsync(msg, kind, cancel)
                    .ConfigureAwait(false);
            }

            var hasErrors = errors.Where(error => !error.IsWarning).Any();

            if (!hasErrors)
            {
                // Apply these functions to engine. 

                string header = "Defined functions:";
                await output.WriteLineAsync(header, OutputKind.Notify, cancel)
                    .ConfigureAwait(false);

                await PrintModuleAsync(module, output, cancel)
                    .ConfigureAwait(false);

                _repl.AddModule(module);
            }

            return FormulaValue.NewVoid();
        }

        internal static async Task PrintModuleAsync(Module module, IReplOutput output, CancellationToken cancel)
        {
            foreach (var funcName in module.Symbols.FunctionNames)
            {
                await output.WriteLineAsync($"  {funcName}", OutputKind.Notify, cancel)
                .ConfigureAwait(false);
            }

            await output.WriteLineAsync(string.Empty, OutputKind.Notify, cancel)
                .ConfigureAwait(false);
        }
    }
}
