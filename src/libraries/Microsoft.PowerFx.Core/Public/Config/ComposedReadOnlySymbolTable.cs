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
    internal class ComposedReadOnlySymbolTable : ReadOnlySymbolTable, INameResolver, IGlobalSymbolNameResolver
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
                
        IExternalDocument INameResolver.Document => default;

        IExternalEntityScope INameResolver.EntityScope => throw new NotImplementedException();

        DName INameResolver.CurrentProperty => default;

        DPath INameResolver.CurrentEntityPath => default;

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

        IExternalEntity INameResolver.CurrentEntity => null;

        public bool SuggestUnqualifiedEnums => false;

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

        public bool LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName)
        {
            dataControlName = default;
            lookupInfo = default;
            return false;
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

        public bool LookupParent(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        public bool LookupSelf(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        public bool TryGetInnermostThisItemScope(out NameLookupInfo nameInfo)
        {
            nameInfo = default;
            return false;
        }

        public bool TryLookupEnum(DName name, out NameLookupInfo lookupInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}
