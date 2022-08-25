// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Glue
{
    [Obsolete("Use symbol table")]
    internal class SimpleResolver : ComposedReadOnlySymbolTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResolver"/> class.
        /// </summary>
        /// <param name="config"></param>        
        public SimpleResolver(PowerFxConfig config)
            : base(new SymbolTableEnumerator(config.SymbolTable))
        {
        }
    }
}
