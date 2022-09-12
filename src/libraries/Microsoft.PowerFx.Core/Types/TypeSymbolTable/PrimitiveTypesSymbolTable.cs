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

namespace Microsoft.PowerFx.Core.Public.Types
{
    internal sealed class PrimitiveTypesSymbolTable : TypeSymbolTable, IGlobalSymbolNameResolver
    {
        private static readonly BidirectionalDictionary<string, FormulaType> _knownTypes = new ()
        {
            { "Boolean", FormulaType.Boolean },
            { "Color", FormulaType.Color },
            { "Date", FormulaType.Date },
            { "DateTime", FormulaType.DateTime },
            { "DateTimeTZ", FormulaType.DateTimeNoTimeZone },
            { "GUID", FormulaType.Guid },
            { "Hyperlink", FormulaType.Hyperlink },
            { "Number", FormulaType.Number },
            { "Text", FormulaType.String },
            { "Time", FormulaType.Time },
            { "None", FormulaType.Blank },
            { "UntypedObject", FormulaType.UntypedObject },
        };

        IReadOnlyDictionary<string, NameLookupInfo> IGlobalSymbolNameResolver.GlobalSymbols => _knownTypes.ToDictionary(kvp => kvp.Key, kvp => ToLookupInfo(kvp.Value));

        internal override VersionHash VersionHash => base.VersionHash;

        private PrimitiveTypesSymbolTable()
        {
        }

        public static PrimitiveTypesSymbolTable Instance = new PrimitiveTypesSymbolTable();

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (!_knownTypes.TryGetFromFirst(name.Value, out var type))
            {
                nameInfo = default;
                return false;
            }

            nameInfo = ToLookupInfo(type);
            return true;
        }

        internal override bool TryGetTypeName(FormulaType type, out string typeName)
        {
            return _knownTypes.TryGetFromSecond(type, out typeName);
        }
    }
}
