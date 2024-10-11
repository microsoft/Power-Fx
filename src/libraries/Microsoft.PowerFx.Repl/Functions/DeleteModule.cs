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
    /// Delete a modules.
    /// </summary>
    internal class DeleteModuleFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public DeleteModuleFunction(PowerFxREPL repl)
            : base("DeleteModule", FormulaType.Void, new[] { FormulaType.String })
        {
            ConfigType = typeof(IReplOutput);
            _repl = repl;
        }

        public async Task<VoidValue> Execute(IReplOutput output, StringValue value, CancellationToken cancel)
        {
            if (!_repl.TryResolveModule(value.Value, out var module))
            {
                await output.WriteLineAsync($"Can't resolve module '{value.Value}'. Try ListModules() to see loaded modules.", OutputKind.Error, cancel)
                .ConfigureAwait(false);
            }
            else
            {
                _repl.DeleteModule(module);

                await output.WriteLineAsync("Removed module: " + module.FullPath, OutputKind.Notify, cancel)
                .ConfigureAwait(false);
            }

            return FormulaValue.NewVoid();
        }
    }
}
