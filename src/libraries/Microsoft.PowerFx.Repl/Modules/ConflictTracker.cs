// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Repl
{
    // Ensure uniqueness 
    internal class ConflictTracker
    {
        // Map from symbol Name to the module it was defined in. 
        private readonly Dictionary<string, Module> _defined = new Dictionary<string, Module>();

        /// <summary>
        /// Verify that the symbols added by Module are unique. 
        /// </summary>
        /// <param name="module"></param>
        /// <exception cref="InvalidOperationException">If a symbol is already defined.</exception>
        public void VerifyUnique(Module module)
        {
            foreach (var name in module.Symbols.FunctionNames)
            {
                if (_defined.TryGetValue(name, out var original))
                {
                    throw new InvalidOperationException($"Symbol '{name}' is already defined in both '{original.FullPath}' and '{module.FullPath}'");
                }

                _defined[name] = module;
            }
        }
    }
}
