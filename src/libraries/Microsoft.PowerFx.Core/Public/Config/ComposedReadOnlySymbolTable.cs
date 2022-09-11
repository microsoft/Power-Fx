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

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Composition of multiple <see cref="ReadOnlySymbolTable"/> into a single table.
    /// </summary>
    internal class ComposedReadOnlySymbolTable : ReadOnlySymbolTable, INameResolver, IGlobalSymbolNameResolver, IEnumStore
    {
        private readonly IEnumerable<ReadOnlySymbolTable> _symbolTables;

        // In priority order. 
        public ComposedReadOnlySymbolTable(SymbolTableEnumerator symbolTables)
        {
            _symbolTables = symbolTables;

            DebugName = string.Join(",", symbolTables.Select(t => t.DebugName));
        }

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

        // Expose the list to aide in intellisense suggestions. 
        IEnumerable<TexlFunction> INameResolver.Functions
        {
            get
            {
                foreach (INameResolver table in _symbolTables)
                {
                    foreach (var function in table.Functions)
                    {
                        yield return function;
                    }
                }
            }
        }

        public IReadOnlyDictionary<string, NameLookupInfo> GlobalSymbols
        {
            get
            {
                var map = new Dictionary<string, NameLookupInfo>();

                foreach (var table in _symbolTables)
                {
                    if (table is IGlobalSymbolNameResolver x)
                    {
                        // Merge into a single dictionary. 
                        foreach (var kv in x.GlobalSymbols)
                        {
                            if (!map.ContainsKey(kv.Key))
                            {
                                map[kv.Key] = kv.Value;
                            }
                        }
                    }
                }

                return map;
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

            // See TexlFunctionsLibrary.Lookup
            var functionLibrary = Functions.Where(func => func.Namespace == theNamespace && name == (localeInvariant ? func.LocaleInvariantName : func.Name)); // Base filter
            return functionLibrary;
        }

        public IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace)
        {
            Contracts.Check(nameSpace.IsValid, "The namespace is invalid.");

            return Functions.Where(function => function.Namespace.Equals(nameSpace));
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
    }
}
