// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Provides symbols to the engine. This includes variables (locals, globals), enums, options sets, and functions.
    /// SymbolTables are mutable to support sessionful scenarios and can be chained together. 
    /// This is a publicly facing class around a <see cref="INameResolver"/>.
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    public class SymbolTable : ReadOnlySymbolTable
    {
        private readonly SlotMap<NameLookupInfo?> _slots = new SlotMap<NameLookupInfo?>();

        /// <summary>
        /// Does this SymbolTable require a corresponding SymbolValue?
        /// True if we have AddVariables, but not needed if we just have constants or functions.
        /// </summary>
        public bool NeedsValues => !_slots.IsEmpty;

        private DName ValidateName(string name)
        {
            if (!DName.IsValidDName(name))
            {
                throw new ArgumentException("Invalid name: ${name}");
            }

            return new DName(name);
        }

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            if (_slots.TryGet(slot.SlotIndex, out var nameInfo))
            {
                return FormulaType.Build(nameInfo.Value.Type);
            }

            throw NewBadSlotException(slot);
        }

        // Ensure that newType can be assigned to the given slot. 
        internal void ValidateAccepts(ISymbolSlot slot, FormulaType newType)
        {
            if (_slots.TryGet(slot.SlotIndex, out var nameInfo))
            {
                var srcType = nameInfo.Value.Type;

                if (newType is RecordType)
                {
                    // Lazy RecordTypes don't validate. 
                    // https://github.com/microsoft/Power-Fx/issues/833
                    return;
                }

                var ok = srcType.Accepts(newType._type);

                if (ok)
                {
                    return;
                }

                var name = (nameInfo.Value.Data as NameSymbol)?.Name;

                throw new InvalidOperationException($"Can't change '{name}' from {srcType} to {newType._type}.");
            }

            throw NewBadSlotException(slot);
        }

        /// <summary>
        /// Provide variable for binding only.
        /// Value must be provided at runtime.
        /// This can throw an exception in case of there is a conflict in name with existing names.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="mutable"></param>
        /// <param name="displayName"></param>
        public ISymbolSlot AddVariable(string name, FormulaType type, bool mutable = false, string displayName = null)
        {
            Inc();
            DName displayDName = default;
            DName varDName = ValidateName(name);

            if (displayName != null)
            {
                displayDName = ValidateName(displayName);
            }

            if (_variables.ContainsKey(name))
            {
                throw new InvalidOperationException($"{name} is already defined");
            }

            var slotIndex = _slots.Alloc();
            var data = new NameSymbol(name, mutable)
            {
                Owner = this,
                SlotIndex = slotIndex
            };

            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data: data,
                displayName: displayDName);

            _slots.SetInitial(slotIndex, info);

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(varDName, displayDName != default ? displayDName : varDName);
            }

            _variables.Add(name, info); // can't exist

            return data;
        }

        /// <summary>
        /// Add a constant.  This is like a variable, but the value is known at bind time. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void AddConstant(string name, FormulaValue data)
        {
            var type = data.Type;

            Inc();
            ValidateName(name);

            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data);

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            // add (logical, logical) pair to display name provider so it still can be included in collision checks.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                var dName = new DName(name);
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(dName, dName);
            }

            _variables.Add(name, info); // can't exist
        }

        /// <summary>
        /// Remove variable, entity or constant of a given name. 
        /// </summary>
        /// <param name="name">display or logical name for the variable or entity to be removed. Logical name of constant to be removed.</param>
        public void RemoveVariable(string name)
        {
            Inc();

            // Also remove from display name provider
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDP)
            {
                var lookupName = new DName(name);

                if (_environmentSymbolDisplayNameProvider.TryGetDisplayName(lookupName, out var displayName))
                {
                    // Do nothing as supplied name was logical name.
                }
                else if (_environmentSymbolDisplayNameProvider.TryGetLogicalName(lookupName, out var logicalName))
                {
                    name = logicalName.Value;
                    lookupName = logicalName;
                }

                _environmentSymbolDisplayNameProvider = ssDP.RemoveField(lookupName);
            }

            if (_variables.TryGetValue(name, out var info))
            {
                if (info.Data is NameSymbol info2)
                {
                    _slots.Remove(info2.SlotIndex);
                    info2.DisposeSlot();
                }
            }

            // Ok to remove if missing. 
            _variables.Remove(name);
        }

        /// <summary>
        /// Remove function of given name. 
        /// </summary>
        /// <param name="name"></param>
        public void RemoveFunction(string name)
        {
            Inc();

            _functions.RemoveAll(func => func.Name == name);
        }

        internal void RemoveFunction(TexlFunction function)
        {
            Inc();

            _functions.RemoveAll(func => func == function);
        }

        internal void AddFunction(TexlFunction function)
        {
            Inc();
            _functions.Add(function);

            // Add any associated enums 
            EnumStoreBuilder?.WithRequiredEnums(new List<TexlFunction>() { function });
        }

        internal EnumStoreBuilder EnumStoreBuilder
        {
            get => _enumStoreBuilder;
            set
            {
                Inc();
                _enumStoreBuilder = value;
            }
        }

        internal void AddEntity(IExternalEntity entity, DName displayName = default)
        {
            Inc();
            NameLookupInfo nameInfo;

            if (entity is IExternalOptionSet optionSet)
            {
                nameInfo = new NameLookupInfo(
                    BindKind.OptionSet,
                    optionSet.Type,
                    DPath.Root,
                    0,
                    optionSet,
                    displayName);
            }
            else if (entity is IExternalDataSource)
            {
                nameInfo = new NameLookupInfo(
                    BindKind.Data,
                    entity.Type,
                    DPath.Root,
                    0,
                    entity,
                    displayName);
            }
            else
            {
                throw new NotImplementedException($"{entity.GetType().Name} not supported.");
            }

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            // For entities without a display name, add (logical, logical) pair to still be included in collision checks.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                displayName = displayName != default ? displayName : entity.EntityName;
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(entity.EntityName, displayName);
            }

            _variables.Add(entity.EntityName, nameInfo);
        }
    }
}
