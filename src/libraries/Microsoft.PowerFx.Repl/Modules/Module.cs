// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Repl
{
    public class Module
    {
        /// <summary>
        /// Public symbols exported by this module. 
        /// </summary>
        public ReadOnlySymbolTable Symbols { get; private set; }

        /// <summary>
        /// Identity of the module. We should never have two different modules with the same identity.
        /// </summary>
        internal ModuleIdentity Identity { get; init; }

        /// <summary>
        /// Optional Full path that this module was loaded from. Null if not loaded from a file.
        /// Primarily useful for helping makers debugging ("where did this module come from").
        /// </summary>
        public string FullPath { get; init; }

        internal Module(ModuleIdentity identity, ReadOnlySymbolTable exports)
        {
            this.Identity = identity;
            this.Symbols = exports ?? throw new ArgumentNullException(nameof(exports));
        }

        public IEnumerable<FunctionInfo> PublicFunctions
        {
            get
            {
                // No other way to enumeraet them all 
#pragma warning disable CS0618 // Type or member is obsolete
                IEnumerable<Core.Functions.TexlFunction> funcs = this.Symbols.Functions.Functions;
#pragma warning restore CS0618 // Type or member is obsolete

                return funcs.Select(func => new FunctionInfo(func));
            }
        }
    }
}
