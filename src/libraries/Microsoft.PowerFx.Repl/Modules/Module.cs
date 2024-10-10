// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Repl
{
    internal class Module
    {
        /// <summary>
        /// Public symbols exported by this module. 
        /// </summary>
        public ReadOnlySymbolTable Symbols { get; private set; }

        /// <summary>
        /// Identity of the module. We should never have two different modules with the same identity.
        /// </summary>
        public ModuleIdentity Identity { get; init; }

        /// <summary>
        /// Optional Full path that this module was loaded from. Null if not loaded from a file.
        /// Primarily useful for helping makers debugging ("where did this module come from")
        /// </summary>
        public string FullPath { get; init; }

        internal Module(ModuleIdentity identity, ReadOnlySymbolTable exports)
        {
            this.Identity = identity;
            this.Symbols = exports ?? throw new ArgumentNullException(nameof(exports));
        }        
    }
}
