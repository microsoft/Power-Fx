// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // Symbol. Public veneer over NameSymbol. 
    [DebuggerDisplay("{BestName}:{Type}")]
    public class SymbolEntry
    {
        public string Name { get; init; }

        public string DisplayName { get; init; }

        /// <summary>
        /// Prefer <see cref="DisplayName"/>if present, else show <see cref="Name"/>. 
        /// </summary>
        public string BestName => string.IsNullOrEmpty(this.DisplayName) ? this.Name : this.DisplayName;

        public SymbolProperties Properties { get; init; }

        public FormulaType Type { get; init; }

        public ISymbolSlot Slot { get; init; }
    }
}
