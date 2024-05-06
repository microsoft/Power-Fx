﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
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
        // Multiple readers ok. But not writing while we read.
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
                    _nameResolverFunctions = new TexlFunctionSet(_symbolTables.Select(t => t.Functions).ToList());
                    _cachedVersionHash = current;
                }

                // Check that it didn't mutate. 
                var newHash = this.VersionHash;
                if (newHash != current)
                {
                    throw new InvalidOperationException($"Symbol Table was mutated during read.");
                }

                return _nameResolverFunctions;                
            }
        }

        // Expose the list to aide in intellisense suggestions.
        IEnumerable<KeyValuePair<DName, FormulaType>> INameResolver.NamedTypes
        {
            get
            {
                var names = new HashSet<string>();
                foreach (INameResolver table in _symbolTables)
                {
                    foreach (var type in table.NamedTypes)
                    {
                        if (names.Add(type.Key))
                        {
                            yield return type;
                        }
                    }
                }
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

        internal override void EnumerateNames(List<SymbolEntry> names, EnumerateNamesOptions opts)
        {
            foreach (var inner in _symbolTables)
            {
                inner?.EnumerateNames(names, opts);
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

        public virtual bool TryLookupEnum(DName name, out NameLookupInfo lookupInfo)
        {
            foreach (INameResolver table in _symbolTables)
            {
                if (table.TryLookupEnum(name, out lookupInfo))
                {
                    return true;
                }
            }

            lookupInfo = default;
            return false;
        }

        public virtual bool LookupType(DName name, out FormulaType fType)
        {
            foreach (INameResolver table in _symbolTables)
            {
                if (table.LookupType(name, out fType))
                {
                    return true;
                }
            }

            fType = default;
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
