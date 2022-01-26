// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    internal class ImmutableEnvironmentSymbolTable
    {
        private readonly ImmutableDictionary<DName, IExternalEntity> _symbols;

        public ImmutableEnvironmentSymbolTable()
        {
            _symbols = ImmutableDictionary<DName, IExternalEntity>.Empty;
        }

        private ImmutableEnvironmentSymbolTable(ImmutableDictionary<DName, IExternalEntity> symbols)
        {
            _symbols = symbols;
        }

        public ImmutableEnvironmentSymbolTable With(IExternalEntity entity)
        {
            return new ImmutableEnvironmentSymbolTable(_symbols.Add(entity.EntityName, entity));
        }

        public bool ContainsSymbol(DName name)
        {
            return _symbols.ContainsKey(name);
        }

        public bool TryGetSymbol(DName name, out IExternalEntity entity)
        {
            return _symbols.TryGetValue(name, out entity);
        }
    }
}
