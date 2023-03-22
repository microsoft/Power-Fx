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
        private readonly bool _mutable;
        private readonly bool _allowThisRecord;

        private readonly NameLookupInfo _thisRecord;

        // Mapping between slots and logical names on RecordType.
        // name --> Slot; used at design time to ensure same slot per field. 
        private readonly Dictionary<string, NameSymbol> _map = new Dictionary<string, NameSymbol>();

        internal RecordType Type => _type;

        public SymbolTableOverRecordType(RecordType type, ReadOnlySymbolTable parent = null, bool mutable = false, bool allowThisRecord = false)
        {
            _type = type;
            _debugName = "per-eval";
            _mutable = mutable;
            _allowThisRecord = allowThisRecord;

            if (_allowThisRecord)
            {
                var data = new NameSymbol(TexlBinding.ThisRecordDefaultName, mutable: false)
                {
                    Owner = this,
                    SlotIndex = int.MaxValue
                };

                _thisRecord = new NameLookupInfo(
                       BindKind.PowerFxResolvedObject,
                       type._type,
                       DPath.Root,
                       0,
                       data: data);
            }
        }

        // Key is the logical name. 
        // Display names are in the NameLookupInfo.DisplayName field.
        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols
        {
            get
            {
                foreach (var kv in _type.GetFieldTypes())
                {
                    yield return new KeyValuePair<string, NameLookupInfo>(kv.Name, Create(kv.Name, kv.Type));
                }

                if (_allowThisRecord)
                {
                    yield return new KeyValuePair<string, NameLookupInfo>(TexlBinding.ThisRecordDefaultName, _thisRecord);
                }
            }
        }

        public bool IsThisRecord(ISymbolSlot slot)
        {
            return slot.SlotIndex == int.MaxValue;
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (_allowThisRecord)
            {
                if (name == TexlBinding.ThisRecordDefaultName)
                {
                    nameInfo = _thisRecord;
                    return true;
                }
            }

            // Lookup must handle display names
            // This is consistent with RowScope. 
            if (_type.TryGetFieldType(name.Value, out var logicalName, out var type))
            {
                nameInfo = Create(logicalName, type);
                return true;
            }

            nameInfo = default;
            return false;
        }

        public FormulaValue GetValue(ISymbolSlot slot, RecordValue value)
        {
            if (IsThisRecord(slot))
            {
                return value;
            }

            var logicalName = GetFieldName(slot);
            var field = value.GetField(logicalName);
            return field;
        }

        public void SetValue(ISymbolSlot slot, RecordValue value, FormulaValue newValue)
        {
            var logicalName = GetFieldName(slot);
            value.UpdateField(logicalName, newValue);
        }

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            var name = GetFieldName(slot);
            _type.TryGetFieldType(name, out var logicalName, out var type);
            return type;
        }

        // Slot was created by this SymbolTable. 
        internal string GetFieldName(ISymbolSlot slot)
        {
            if (slot is NameSymbol data)
            {
                return data.Name;
            }

            throw NewBadSlotException(slot); 
        }

        private NameLookupInfo Create(string logicalName, FormulaType type)
        {
            var hasDisplayName = DType.TryGetDisplayNameForColumn(_type._type, logicalName, out var displayName);

            NameSymbol data;
            lock (_map)
            {
                if (!_map.TryGetValue(logicalName, out data))
                {
                    // Slot is based on map count, so whole operation needs to be under single lock. 
                    var slotIdx = _map.Count;

                    data = new NameSymbol(logicalName, _mutable)
                    {
                        Owner = this,
                        SlotIndex = slotIdx
                    };
                    _map.Add(logicalName, data);
                }
            }

            return new NameLookupInfo(
                   BindKind.PowerFxResolvedObject,
                   type._type,
                   DPath.Root,
                   0,
                   data: data,
                   displayName: hasDisplayName ? new DName(displayName) : default);
        }
    }
}
