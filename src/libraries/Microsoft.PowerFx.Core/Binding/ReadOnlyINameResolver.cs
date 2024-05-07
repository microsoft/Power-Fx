// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    // Helper class to create composable INameResolver
    internal class ReadOnlyINameResolver : ReadOnlySymbolTable, INameResolver
    {
        private readonly INameResolver _nameResolver;

        public ReadOnlyINameResolver(INameResolver nameResolver) 
        {
            Contracts.AssertValue(nameResolver);

            _nameResolver = nameResolver;
        }

        bool INameResolver.LookupType(DName name, out FormulaType fType)
        {
            Contracts.AssertValid(name);
            Contracts.AssertNonEmpty(name);

            return _nameResolver.LookupType(name, out fType);
        }
    }
}
