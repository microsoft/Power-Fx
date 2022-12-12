// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Runtime configuration for the execution of an expression.
    /// </summary>
    public class RuntimeConfig
    {
        /// <summary>
        /// This should match the SymbolTable provided at bind time. 
        /// </summary>
        public ReadOnlySymbolValues Values { get; set; }

        public IServiceProvider Services { get; set; }

        // Other services:
        // CultureInfo, Timezone, Clock, 
        // Stack depth 
        // Max memory 
        // Logging
    }
}
