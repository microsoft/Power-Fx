// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
        [ThreadSafeProtectedByLock("_map")]
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
                var data = new NameSymbol(TexlBinding.ThisRecordDefaultName, new SymbolProperties
                {
                     CanMutate = false,
                     CanSet = false
                })
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolTableOverRecordType"/> class.
        /// NOTE: Use this to block implicit ThisRecord.
        /// </summary>
        internal SymbolTableOverRecordType(RecordType type, ReadOnlySymbolTable parent = null, bool mutable = false)
        {
            _type = RecordType.Empty();
            _debugName = "per-eval";
            _mutable = mutable;
            _allowThisRecord = true;

            var data = new NameSymbol(TexlBinding.ThisRecordDefaultName, new SymbolProperties
            {
                CanMutate = false,
                CanSet = false
            })
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

        // Key is the logical name. 
        // Display names are in the NameLookupInfo.DisplayName field.
        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols
        {
            get
            {
                // Don't build type if row scope has external sources as that can take a lot of time and hang intellisense
                // https://github.com/microsoft/Power-Fx/issues/1212
                if (_type._type.AssociatedDataSources.Any())
                {
                    foreach (var field in _type.FieldNames)
                    {
                        DType.TryGetDisplayNameForColumn(_type._type, field, out var displayName);
                        DName dName = default;
                        if (DName.IsValidDName(displayName))
                        {
                            dName = new DName(displayName);
                        }

                        var info = new NameLookupInfo(BindKind.TypeName, DType.Deferred, DPath.Root, 0, displayName: dName);
                        yield return new KeyValuePair<string, NameLookupInfo>(field, info);
                    }
                }
                else
                {
                    foreach (var kv in _type.GetFieldTypes())
                    {
                        yield return new KeyValuePair<string, NameLookupInfo>(kv.Name, Create(kv.Name, kv.Type));
                    }
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
                
        bool INameResolver.Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences)
        {
            if (preferences.HasFlag(NameLookupPreferences.GlobalsOnly))
            {
                // Global, specified by [@name]  syntax, mean we skip RowScope. 

                nameInfo = default;
                return false;
            }
        
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

                    data = new NameSymbol(logicalName, new SymbolProperties
                    {
                        CanSet = _mutable,
                        CanMutate = false
                    })
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
