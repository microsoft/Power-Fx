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
    /// <summary>
    /// Helper class to create composable INameResolver. Only supports LookupType method currently.
    /// Other INameResolver methods should be implemented as needed.
    /// </summary>
    internal class ReadOnlyNameResolver : ReadOnlySymbolTable, INameResolver
    {
        private readonly INameResolver _nameResolver;

        public ReadOnlyNameResolver(INameResolver nameResolver) 
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
