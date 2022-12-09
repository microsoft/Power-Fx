// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.App;
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
    /// ReadOnly version of a Symbol Table. This feeds functions, variables, enums, etc into
    /// the binder.   
    /// See <see cref="SymbolTable"/> for mutable version. 
    /// </summary>
    [DebuggerDisplay("{_debugName}_{GetHashCode()}")]
    [ThreadSafeImmutable]
    public abstract class ReadOnlySymbolTable : INameResolver, IGlobalSymbolNameResolver, IEnumStore
    {
        // Changed on each update. 
        // Host can use to ensure that a symbol table wasn't mutated on us.                 
        private protected VersionHash _version = VersionHash.New();

        /// <summary>
        /// This can be compared to determine if the symbol table was mutated during an operation. 
        /// </summary>
        internal virtual VersionHash VersionHash => _parent == null ?
            _version : _version.Combine(_parent.VersionHash);

        /// <summary>
        /// Notify the symbol table has changed. 
        /// </summary>
        public void Inc()
        {
            _version.Inc();
        }

        private protected ReadOnlySymbolTable _parent;

        private protected string _debugName = "SymbolTable";

        // Helper in debugging. Useful when we have multiple symbol tables chained. 
        public string DebugName
        {
            get => _debugName;
            init => _debugName = value;
        }

        // Should remove. 
        // https://github.com/microsoft/Power-Fx/issues/828
        [Obsolete("Use Composition instead of Parent Pointer")]
        public ReadOnlySymbolTable Parent => _parent;

        internal virtual IEnumerable<ReadOnlySymbolTable> SubTables
        {
            get
            {
                if (_parent == null)
                {
                    return new ReadOnlySymbolTable[] { this };
                }
                else
                {
                    return new ReadOnlySymbolTable[] { this, _parent };
                }
            }
        }

        /// <summary>
        /// Find the variable by name within this symbol table. 
        /// </summary>
        /// <param name="name">name of the variable.</param>
        /// <param name="slot">slot allocated for this variable.</param>
        /// <returns></returns>
        public bool TryLookupSlot(string name, out ISymbolSlot slot)
        {
            INameResolver resolver = this;

            if (resolver.Lookup(new DName(name), out var info))
            {
                if (info.Data is NameSymbol data)
                {
                    slot = data;
                    return true;
                }
            }

            slot = null;
            return false;
        }

        /// <summary>
        /// Given a valid slot, get the type. 
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            ValidateSlot(slot);

            IGlobalSymbolNameResolver resolver = this;
            foreach (var x in resolver.GlobalSymbols)
            {
                if (x.Value.Data is ISymbolSlot slot2)
                {
                    if (slot2.SlotIndex == slot.SlotIndex) 
                    {
                        return FormulaType.Build(x.Value.Type);
                    }
                }
            }

            throw NewBadSlotException(slot);
        }

        // Helper to call on Get/Set to ensure slot can be used with this value
        internal void ValidateSlot(ISymbolSlot slot)
        {
            if (slot.Owner != this)
            {
                throw NewBadSlotException(slot);
            }

            slot.ThrowIfDisposed();
        }

        internal Exception NewBadSlotException(ISymbolSlot slot)
        {
            return new InvalidOperationException($"Slot {slot.DebugName()} is not valid on Symbol Table {this.DebugName()}");
        }

        public static ReadOnlySymbolTable NewFromRecord(
            RecordType type,
            string debugName = null,
            bool allowThisRecord = false,
            bool allowMutable = false)
        {
            return new SymbolTableOverRecordType(type ?? RecordType.Empty(), null, mutable: allowMutable, allowThisRecord: allowThisRecord)
            {
                DebugName = debugName ?? (allowThisRecord ? "RowScope" : "FromRecord")
            };
        }

        public static ReadOnlySymbolTable Compose(params ReadOnlySymbolTable[] tables)
        {
            return new ComposedReadOnlySymbolTable(new SymbolTableEnumerator(tables));
        }

        // Helper to create a ReadOnly symbol table around a set of core functions.
        // Important that this is readonly so that it can be safely shared across engines. 
        internal static ReadOnlySymbolTable NewDefault(IEnumerable<TexlFunction> coreFunctions)
        {
            var s = new SymbolTable
            {
                EnumStoreBuilder = new EnumStoreBuilder(),
                DebugName = $"BuiltinFunctions ({coreFunctions.Count()})"
            };

            foreach (var func in coreFunctions)
            {
                s.AddFunction(func); // will also add enum. 
            }

            return s;
        }

        /// <summary>
        /// Helper to create a symbol table around a set of core functions.
        /// Important that this is mutable so that it can be changed across engines. 
        /// </summary>
        /// <returns>SymbolTable with supported functions.</returns>
        public SymbolTable GetMutableCopyOfFunctions()
        {
            var s = new SymbolTable()
            {
                DebugName = DebugName + " (Functions only)",
#pragma warning disable CS0618 // Type or member is obsolete
                Parent = Parent,
#pragma warning restore CS0618 // Type or member is obsolete
            };

            foreach (var func in _functions)
            {
                s.AddFunction(func); 
            }

            return s;
        }

        // https://github.com/microsoft/Power-Fx/issues/779
        // _environmentSymbols have the display name support (they update the _environmentSymbolDisplayNameProvider)
        // Merge _variables with _environmentSymbols to provide display name support for variables.. 
        // Both ultimately serve in INameResolver.Lookup 
        private protected readonly Dictionary<string, NameLookupInfo> _variables = new Dictionary<string, NameLookupInfo>();

        internal readonly Dictionary<DName, IExternalEntity> _environmentSymbols = new Dictionary<DName, IExternalEntity>();

        internal DisplayNameProvider _environmentSymbolDisplayNameProvider = new SingleSourceDisplayNameProvider();

        private protected readonly List<TexlFunction> _functions = new List<TexlFunction>();

        // Which enums are available. 
        // These do not compose - only bottom one wins. 
        // ComposedReadOnlySymbolTable will handle composition by looking up in each symbol table. 
        private protected EnumStoreBuilder _enumStoreBuilder;
        private EnumSymbol[] _enumSymbolCache;

        private EnumSymbol[] GetEnumSymbolSnapshot
        {
            get
            {
                // The caller may add to the builder after we've assigned. 
                // So delay snapshot until we actually need to read it. 
                if (_enumStoreBuilder == null)
                {
                    _enumSymbolCache = new EnumSymbol[] { };
                }

                if (_enumSymbolCache == null)
                {
                    _enumSymbolCache = _enumStoreBuilder.Build().EnumSymbols.ToArray();
                }

                return _enumSymbolCache;
            }
        }

        IEnumerable<EnumSymbol> IEnumStore.EnumSymbols => GetEnumSymbolSnapshot;

        internal IEnumerable<TexlFunction> Functions => ((INameResolver)this).Functions;

        IEnumerable<TexlFunction> INameResolver.Functions => _functions; 
        
        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols => _variables;

        /// <summary>
        /// Get symbol names in this current scope.
        /// </summary>
        public IEnumerable<NamedFormulaType> SymbolNames
        {
            get 
            {
                IGlobalSymbolNameResolver globals = this;
                
                // GlobalSymbols are virtual, so we get derived behavior via that.
                foreach (var kv in globals.GlobalSymbols)
                {
                    var type = FormulaType.Build(kv.Value.Type);
                    yield return new NamedFormulaType(kv.Key, type);
                }
            }
        }

        internal string GetSuggestableSymbolName(IExternalEntity entity)
        {
            var name = entity.EntityName;
            if (_environmentSymbolDisplayNameProvider.TryGetDisplayName(name, out var displayName))
            {
                return displayName.Value;
            }

            return name.Value;
        }

        internal bool TryGetSymbol(DName name, out IExternalEntity symbol, out DName displayName)
        {
            var lookupName = name;
            if (_environmentSymbolDisplayNameProvider.TryGetDisplayName(name, out displayName))
            {
                lookupName = name;
            }
            else if (_environmentSymbolDisplayNameProvider.TryGetLogicalName(name, out var logicalName))
            {
                lookupName = logicalName;
                displayName = name;
            }

            return _environmentSymbols.TryGetValue(lookupName, out symbol);
        }

        // Derived symbol tables can hook. 
        // NameLookupPreferences is just for legacy lookup behavior, so we don't need to pass it to this hook
        internal virtual bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            nameInfo = default;
            return false;
        }

        bool INameResolver.Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences)
        {
            if (TryLookup(name, out nameInfo))
            {
                return true;
            }

            if (_variables.TryGetValue(name.Value, out nameInfo))
            {
                return true;
            }

            // This does a display-name aware lookup from _environmentSymbols 
            if (TryGetSymbol(name, out var symbol, out var displayName))
            {
                // Special case symbols
                if (symbol is IExternalOptionSet optionSet)
                {
                    nameInfo = new NameLookupInfo(
                        BindKind.OptionSet,
                        optionSet.Type,
                        DPath.Root,
                        0,
                        optionSet,
                        displayName);

                    return true;
                }
                else if (symbol is IExternalDataSource)
                {
                    nameInfo = new NameLookupInfo(
                        BindKind.Data,
                        symbol.Type,
                        DPath.Root,
                        0,
                        symbol,
                        displayName);

                    return true;
                }
                else
                {
                    throw new NotImplementedException($"{symbol.GetType().Name} not supported.");
                }
            }

            var enumValue = GetEnumSymbolSnapshot.FirstOrDefault(symbol => symbol.InvariantName == name);
            if (enumValue != null)
            {
                nameInfo = new NameLookupInfo(BindKind.Enum, enumValue.EnumType, DPath.Root, 0, enumValue);
                return true;
            }

            nameInfo = default;
            return false;
        }

        IEnumerable<TexlFunction> INameResolver.LookupFunctions(DPath theNamespace, string name, bool localeInvariant)
        {
            Contracts.Check(theNamespace.IsValid, "The namespace is invalid.");
            Contracts.CheckNonEmpty(name, "name");

            // See TexlFunctionsLibrary.Lookup
            // return _functionLibrary.Lookup(theNamespace, name, localeInvariant, null);            
            var functionLibrary = _functions.Where(func => func.Namespace == theNamespace && name == (localeInvariant ? func.LocaleInvariantName : func.Name)); // Base filter
            return functionLibrary;
        }

        IEnumerable<TexlFunction> INameResolver.LookupFunctionsInNamespace(DPath nameSpace)
        {
            Contracts.Check(nameSpace.IsValid, "The namespace is invalid.");

            return _functions.Where(function => function.Namespace.Equals(nameSpace));
        }

        bool INameResolver.LookupEnumValueByInfoAndLocName(object enumInfo, DName locName, out object value)
        {
            value = null;
            var castEnumInfo = enumInfo as EnumSymbol;
            return castEnumInfo?.TryLookupValueByLocName(locName.Value, out _, out value) ?? false;
        }

        bool INameResolver.LookupEnumValueByTypeAndLocName(DType enumType, DName locName, out object value)
        {
            // Slower O(n) lookup involving a walk over the registered enums...
            foreach (var info in GetEnumSymbolSnapshot)
            {
                if (info.EnumType == enumType)
                {
                    return info.TryLookupValueByLocName(locName.Value, out _, out value);
                }
            }

            value = null;
            return false;
        }

        #region INameResolver - not implemented

        // Methods from INameResolver that we default / don't implement
        IExternalDocument INameResolver.Document => default;

        IExternalEntityScope INameResolver.EntityScope => default;

        IExternalEntity INameResolver.CurrentEntity => default;

        DName INameResolver.CurrentProperty => default;

        DPath INameResolver.CurrentEntityPath => default;

        bool INameResolver.SuggestUnqualifiedEnums => false;

        bool INameResolver.LookupParent(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        bool INameResolver.LookupSelf(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        bool INameResolver.LookupGlobalEntity(DName name, out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        bool INameResolver.TryLookupEnum(DName name, out NameLookupInfo lookupInfo)
        {
            throw new NotImplementedException();
        }

        bool INameResolver.TryGetInnermostThisItemScope(out NameLookupInfo nameInfo)
        {
            nameInfo = default;
            return false;
        }

        bool INameResolver.LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName)
        {
            dataControlName = default;
            lookupInfo = default;
            return false;
        }
        #endregion
    }
}
