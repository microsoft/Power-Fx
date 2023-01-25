// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Composition of multiple <see cref="ReadOnlySymbolTable"/> into a single table.
    /// </summary>
    internal class ComposedReadOnlySymbolTable : ReadOnlySymbolTable, INameResolver, IGlobalSymbolNameResolver, IEnumStore
    {
        private readonly IEnumerable<ReadOnlySymbolTable> _symbolTables;

        // In priority order. 
        public ComposedReadOnlySymbolTable(params ReadOnlySymbolTable[] symbolTables)
        {
            _symbolTables = symbolTables.Where(x => x != null);

            DebugName = "(" + string.Join(",", _symbolTables.Select(t => t.DebugName)) + ")";
        }

        internal IEnumerable<ReadOnlySymbolTable> SubTables => _symbolTables;

        internal override VersionHash VersionHash
        {
            get
            {
                var hash = new VersionHash();
                foreach (var table in _symbolTables)
                {
                    hash = hash.Combine(table.VersionHash);
                }

                return hash;
            }
        }

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            if (slot.Owner == this)
            {
                // A slot's owner must be a "leaf node" symbol table and note 
                // a composed type.
                // Check to avoid recursion.
                throw new InvalidOperationException("Slot has illegal owner.");
            }

            return slot.Owner.GetTypeFromSlot(slot);
        }

        private TexlFunctionSet _nameResolverFunctions = null;
        private VersionHash _cachedVersionHash = VersionHash.New();

        // Expose the list to aide in intellisense suggestions. 
        TexlFunctionSet INameResolver.Functions
        {
            get
            {
                var current = this.VersionHash;
                if (current != _cachedVersionHash)
                {
                    _nameResolverFunctions = null;       
                }

                if (_nameResolverFunctions == null)
                {
                    _nameResolverFunctions = new TexlFunctionSet(_symbolTables.Select(t => t.Functions));
                    _cachedVersionHash = current;
                }

                return _nameResolverFunctions;                
            }
        }

        public IEnumerable<KeyValuePair<string, NameLookupInfo>> GlobalSymbols
        {
            get
            {
                var names = new HashSet<string>();
                var map = new List<KeyValuePair<string, NameLookupInfo>>();

                foreach (var table in _symbolTables)
                {
                    if (table is IGlobalSymbolNameResolver globalSymbolNameResolver)
                    {
                        foreach (var symbol in globalSymbolNameResolver.GlobalSymbols)
                        {
                            if (names.Add(symbol.Key))
                            {
                                yield return symbol;
                            }
                        }
                    }
                }
            }
        }

        IEnumerable<EnumSymbol> IEnumStore.EnumSymbols
        {
            get
            {
                foreach (var table in _symbolTables)
                {
                    if (table is IEnumStore store)
                    {
                        foreach (var item in store.EnumSymbols)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        public virtual bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
        {
            foreach (INameResolver table in _symbolTables)
            {
                if (table.Lookup(name, out nameInfo, preferences))
                {
                    return true;
                }
            }

            nameInfo = default;
            return false;
        }

        public virtual IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name, bool localeInvariant = false)
        {
            Contracts.Check(theNamespace.IsValid, "The namespace is invalid.");
            Contracts.CheckNonEmpty(name, "name");

            return localeInvariant
                        ? Functions.WithInvariantName(name, theNamespace)
                        : Functions.WithName(name, theNamespace);
        }

        public IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace)
        {
            Contracts.Check(nameSpace.IsValid, "The namespace is invalid.");
            return Functions.WithNamespace(nameSpace);
        }

        public virtual bool LookupEnumValueByInfoAndLocName(object enumInfo, DName locName, out object value)
        {
            value = null;
            var castEnumInfo = enumInfo as EnumSymbol;
            return castEnumInfo?.TryLookupValueByLocName(locName.Value, out _, out value) ?? false;
        }

        public virtual bool LookupEnumValueByTypeAndLocName(DType enumType, DName locName, out object value)
        {
            // Slower O(n) lookup involving a walk over the registered enums...
            foreach (INameResolver table in _symbolTables)
            {
                if (table.LookupEnumValueByTypeAndLocName(enumType, locName, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        public virtual bool LookupGlobalEntity(DName name, out NameLookupInfo lookupInfo)
        {
            foreach (INameResolver table in _symbolTables)
            {
                if (table.LookupGlobalEntity(name, out lookupInfo))
                {
                    return true;
                }
            }

            lookupInfo = default;
            return false;
        }

        internal override IExternalEntityScope InternalEntityScope
        {
            get
            {
                // returns the first EntityScope from composed tables
                // intended for unit testing purposes
                foreach (INameResolver table in _symbolTables)
                {
                    if (table.EntityScope != null)
                    {
                        return table.EntityScope;
                    }
                }

                return default;
            }
        }
    }
}
