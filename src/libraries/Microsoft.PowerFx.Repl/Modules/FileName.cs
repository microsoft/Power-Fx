// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Repl
{
    public static class ModuleHelper
    {
        // $$$ Potentially confusing - this does not actually update engine symbols...
        public static async Task<Module> LoadModuleAsync(this RecalcEngine engine, string fullpath)
        {
            var commonIncomingSymbols = engine.GetCombinedEngineSymbols();
            var module = await LoadModuleAsync(fullpath, commonIncomingSymbols)
                .ConfigureAwait(false);

            return module;
        }

        public static async Task<Module> LoadModuleAsync(string fullpath, ReadOnlySymbolTable commonIncomingSymbols)
        {
            if (!Path.IsPathRooted(fullpath))
            {
                throw new ArgumentException($"Path must be rooted");
            }

            var ctx = new ModuleLoadContext(commonIncomingSymbols);

            var errors = new List<ExpressionError>();

            var module = await ctx.LoadFromFileAsync(fullpath, errors).ConfigureAwait(false);
            var hasErrors = errors.Where(error => !error.IsWarning).Any();

            if (hasErrors)
            {
                throw new InvalidOperationException($"Module has error");
            }

            return module;
        }
    }
}
