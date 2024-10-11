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
    /// List the modules that are loaded.
    /// </summary>
    internal class ListModulesFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public ListModulesFunction(PowerFxREPL repl)
            : base("ListModules", FormulaType.Void)
        {
            ConfigType = typeof(IReplOutput);
            _repl = repl;
        }

        public async Task<VoidValue> Execute(IReplOutput output, CancellationToken cancel)
        {
            var modules = _repl.Modules;

            await output.WriteLineAsync("Modules loaded:", OutputKind.Notify, cancel)
                .ConfigureAwait(false);

            foreach (var module in modules)
            {
                await output.WriteLineAsync(module.FullPath, OutputKind.Notify, cancel)
                    .ConfigureAwait(false);

                await ImportFunction.PrintModuleAsync(module, output, cancel)
                    .ConfigureAwait(false);
            }

            return FormulaValue.NewVoid();
        }
    }
}
