// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class DefinedTypeSymbolTable : TypeSymbolTable, IGlobalSymbolNameResolver
    {
        private static readonly BidirectionalDictionary<string, FormulaType> _definedTypes = new ();

        IReadOnlyDictionary<string, NameLookupInfo> IGlobalSymbolNameResolver.GlobalSymbols => _definedTypes.ToDictionary(kvp => kvp.Key, kvp => ToLookupInfo(kvp.Value));

        internal void RegisterType(string typeName, AggregateType type)
        {
            Inc();            

            _definedTypes.Add(typeName, type);
        }

        protected void ValidateName(string name)
        {
            if (!DName.IsValidDName(name))
            {
                throw new ArgumentException("Invalid name: ${name}");
            }
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (!_definedTypes.TryGetFromFirst(name.Value, out var type))
            {
                nameInfo = default;
                return false;
            }

            nameInfo = ToLookupInfo(type);
            return true;
        }

        internal override bool TryGetTypeName(FormulaType type, out string typeName)
        {
            return _definedTypes.TryGetFromSecond(type, out typeName);
        }
    }
}
