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
using Microsoft.PowerFx.Core.Texl.Builtins;
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
        internal virtual VersionHash VersionHash => _version;

        /// <summary>
        /// Notify the symbol table has changed. 
        /// </summary>
        public void Inc()
        {
            _version.Inc();
        }

        private protected string _debugName = "SymbolTable";

        // Helper in debugging. Useful when we have multiple symbol tables chained. 
        public string DebugName
        {
            get => _debugName;
            init => _debugName = value;
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

        // Ensure that newType can be assigned to the given slot. 
        internal void ValidateAccepts(ISymbolSlot slot, FormulaType newType)
        {
            var srcType = this.GetTypeFromSlot(slot);

            if (newType is RecordType)
            {
                // Lazy RecordTypes don't validate. 
                // https://github.com/microsoft/Power-Fx/issues/833
                return;
            }

            var ok = srcType._type.Accepts(newType._type);

            if (ok)
            {
                return;
            }

            var name = slot.DebugName();

            throw new InvalidOperationException($"Can't change '{name}' from {srcType} to {newType._type}.");
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

        /// <summary>
        /// Create a symbol table around the DisplayNameProvider. 
        /// The set of symbols is fixed and determined by the DisplayNameProvider, 
        /// but their type info is lazily hydrated. 
        /// </summary>
        /// <returns></returns>
        public static ReadOnlySymbolTable NewFromDeferred(
            DisplayNameProvider map,
            Func<string, string, FormulaType> fetchTypeInfo,
            string debugName = null)
        {
            return new DeferredSymbolTable(map, fetchTypeInfo)
            {
                DebugName = debugName
            };
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
            return new ComposedReadOnlySymbolTable(tables);
        }

        // Helper to create a ReadOnly symbol table around a set of core functions.
        // Important that this is readonly so that it can be safely shared across engines. 
        internal static ReadOnlySymbolTable NewDefault(TexlFunctionSet coreFunctions)
        {
            var s = new SymbolTable
            {
                EnumStoreBuilder = new EnumStoreBuilder(),
                DebugName = $"BuiltinFunctions ({coreFunctions.Count()})"
            };

            s.AddFunctions(coreFunctions);

            return s;
        }

        internal static ReadOnlySymbolTable NewDefault(IEnumerable<TexlFunction> functions)
        {            
            TexlFunctionSet tfs = new TexlFunctionSet();

            foreach (TexlFunction function in functions)
            {
                tfs.Add(function);
            }

            return NewDefault(tfs);
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
            };

            s.AddFunctions(_functions);

            return s;
        }

        internal readonly Dictionary<string, NameLookupInfo> _variables = new Dictionary<string, NameLookupInfo>();

        private protected readonly TexlFunctionSet _functions = new TexlFunctionSet();

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

        internal TexlFunctionSet Functions => ((INameResolver)this).Functions;

        TexlFunctionSet INameResolver.Functions => _functions;

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
                    var displayName = kv.Value.DisplayName != default ? kv.Value.DisplayName.Value : null;
                    yield return new NamedFormulaType(kv.Key, type, displayName);
                }
            }
        }

        // Hook from Lookup - Get just variables. 
        internal virtual bool TryGetVariable(DName name, out NameLookupInfo symbol, out DName displayName)
        {
            symbol = default;
            displayName = default;
            return false;
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

            // This does a display-name aware lookup from _variables 
            if (TryGetVariable(name, out nameInfo, out _))
            {
                return true;
            }

            var enumValue = GetEnumSymbolSnapshot.FirstOrDefault(symbol => symbol.EntityName.Value == name);
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

            return localeInvariant 
                        ? Functions.WithInvariantName(name, theNamespace) 
                        : Functions.WithName(name, theNamespace);            
        }
        
        IEnumerable<TexlFunction> INameResolver.LookupFunctionsInNamespace(DPath nameSpace)
        {
            Contracts.Check(nameSpace.IsValid, "The namespace is invalid.");
            
            return _functions.WithNamespace(nameSpace);
        }

        #region INameResolver - only implemented for unit testing for scenarios that use the full name resolver

        internal virtual IExternalEntityScope InternalEntityScope => default;

        IExternalEntityScope INameResolver.EntityScope => InternalEntityScope;

        #endregion

        #region INameResolver - not implemented

        // Methods from INameResolver that we default / don't implement
        IExternalDocument INameResolver.Document => default;

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
