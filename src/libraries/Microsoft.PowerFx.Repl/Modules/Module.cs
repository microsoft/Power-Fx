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
        /// Full path that this module was loaded from. Null if not loaded from a file.
        /// </summary>
        public string FullPath { get; init; }

        internal Module(ReadOnlySymbolTable exports)
        {
            this.Symbols = exports ?? throw new ArgumentNullException(nameof(exports));
        }        
    }
}
