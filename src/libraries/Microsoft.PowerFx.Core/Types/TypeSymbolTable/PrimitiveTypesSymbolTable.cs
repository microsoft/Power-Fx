// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    [ThreadSafeImmutable]
    internal sealed class PrimitiveTypesSymbolTable : TypeSymbolTable, IGlobalSymbolNameResolver
    {
        private static readonly IReadOnlyDictionary<string, FormulaType> _knownTypes = new Dictionary<string, FormulaType>()
        {
            { "Boolean", FormulaType.Boolean },
            { "Color", FormulaType.Color },
            { "Date", FormulaType.Date },
            { "Time", FormulaType.Time },
            { "DateTime", FormulaType.DateTime },
            { "DateTimeTZInd", FormulaType.DateTimeNoTimeZone },
            { "GUID", FormulaType.Guid },
            { "Number", FormulaType.Number },
            { "Text", FormulaType.String },
            { "Hyperlink", FormulaType.Hyperlink },
            { "None", FormulaType.Blank },
            { "UntypedObject", FormulaType.UntypedObject },
        };

        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols => _knownTypes.ToDictionary(kvp => kvp.Key, kvp => ToLookupInfo(kvp.Value));

        private PrimitiveTypesSymbolTable()
        {
        }

        public static readonly PrimitiveTypesSymbolTable Instance = new PrimitiveTypesSymbolTable();

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (!_knownTypes.TryGetValue(name.Value, out var type))
            {
                nameInfo = default;
                return false;
            }

            nameInfo = ToLookupInfo(type);
            return true;
        }

        internal override bool TryGetTypeName(FormulaType type, out string typeName)
        {
            typeName = _knownTypes.Where(kvp => kvp.Value.Equals(type)).FirstOrDefault().Key;

            return typeName != null;
        }
    }
}
