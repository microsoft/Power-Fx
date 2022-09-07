// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // Wraps a record
    // This also preserves the lazy field enumeration semantics of a recordType.
    internal class SymbolTableOverRecordType : ReadOnlySymbolTable, IGlobalSymbolNameResolver
    {
        private readonly RecordType _type;

        public SymbolTableOverRecordType(RecordType type, ReadOnlySymbolTable parent = null)
        {
            _type = type;
            _debugName = "per-eval";
            _parent = parent;
        }

        // Key is the logical name. 
        // Display names are in the NameLookupInfo.DisplayName field.
        IReadOnlyDictionary<string, NameLookupInfo> IGlobalSymbolNameResolver.GlobalSymbols
        {
            get
            {
                var map = new Dictionary<string, NameLookupInfo>();
                foreach (var kv in _type.GetFieldTypes())
                {
                    map[kv.Name] = Create(kv.Name, kv.Type);
                }

                return map;
            }
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            // Lookup must handle display names
            // This is consistent with RowScope. 
            if (!DType.TryGetConvertedDisplayNameAndLogicalNameForColumn(_type._type, name, out var logicalName, out _))
            {
                logicalName = name.Value;
            }

            if (_type.TryGetFieldType(logicalName, out var type))
            {
                nameInfo = Create(name.Value, type);
                return true;
            }

            nameInfo = default;
            return false;
        }

        private NameLookupInfo Create(string name, FormulaType type)
        {
            var hasDisplayName = DType.TryGetDisplayNameForColumn(_type._type, name, out var displayName);

            return new NameLookupInfo(
                   BindKind.PowerFxResolvedObject,
                   type._type,
                   DPath.Root,
                   0,
                   data: new NameSymbol(name),
                   displayName: hasDisplayName ? new DName(displayName) : default);
        }
    }
}
