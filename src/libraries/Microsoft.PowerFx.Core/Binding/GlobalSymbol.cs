// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Binding
{
    internal class GlobalSymbol : IGlobalSymbol
    {
        public string Name { get; private set; }

        public string Description { get; private set; }

        public FormulaType Type { get; private set; }

        public GlobalSymbol(string name, string description, FormulaType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
