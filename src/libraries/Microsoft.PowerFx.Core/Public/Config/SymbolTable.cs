// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Annotations;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Provides symbols to the engine. This includes variables (locals, globals), enums, options sets, and functions.
    /// SymbolTables are mutable to support sessionful scenarios and can be chained together. 
    /// This is a publicly facing class around a <see cref="INameResolver"/>.
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    [NotThreadSafe]
    public class SymbolTable : ReadOnlySymbolTable, IGlobalSymbolNameResolver
    {
        private readonly GuardSingleThreaded _guard = new GuardSingleThreaded();

        private readonly SlotMap<NameLookupInfo?> _slots = new SlotMap<NameLookupInfo?>();

        private DisplayNameProvider _environmentSymbolDisplayNameProvider = new SingleSourceDisplayNameProvider();

        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols => _variables;

        internal const string UserInfoSymbolName = "User";

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

        internal override bool TryGetVariable(DName name, out NameLookupInfo symbol, out DName displayName)
        {
            var lookupName = name;

            if (_environmentSymbolDisplayNameProvider.TryGetDisplayName(name, out displayName))
            {
                // do nothing as provided name can be used for lookup with logical name
            }
            else if (_environmentSymbolDisplayNameProvider.TryGetLogicalName(name, out var logicalName))
            {
                lookupName = logicalName;
                displayName = name;
            }

            return _variables.TryGetValue(lookupName, out symbol);
        }

        // Exists for binary backcompat.
        public ISymbolSlot AddVariable(string name, FormulaType type, bool mutable = false, string displayName = null)
        {
            var props = new SymbolProperties
            {
                CanSet = mutable,
                CanMutate = mutable
            };
            return AddVariable(name, type, props, displayName);
        }

        /// <summary>
        /// Provide variable for binding only.
        /// Value must be provided at runtime.
        /// This can throw an exception in case of there is a conflict in name with existing names.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="props"></param>
        /// <param name="displayName"></param>
        public ISymbolSlot AddVariable(string name, FormulaType type, SymbolProperties props, string displayName = null)
        {
            if (props == null)
            {
                // Default.
                props = new SymbolProperties();
            }

            using var guard = _guard.Enter(); // Region is single threaded.

            Inc();
            DName displayDName = default;
            DName varDName = ValidateName(name);

            if (type is Types.Void)
            {
                throw new InvalidOperationException($"Can't set {name} to a Void value.");
            }

            if (displayName != null)
            {
                displayDName = ValidateName(displayName);
            }

            if (_variables.ContainsKey(name))
            {
                throw new InvalidOperationException($"{name} is already defined");
            }

            var slotIndex = _slots.Alloc();
            var data = new NameSymbol(name, props)
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
            using var guard = _guard.Enter(); // Region is single threaded.

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
        /// Adds an user defined function.
        /// </summary>
        /// <param name="script">String representation of the user defined function.</param>
        /// <param name="parseCulture">CultureInfo to parse the script againts. Default is invariant.</param>
        /// <param name="symbolTable">Extra symbols to bind UDF. Commonly coming from Engine.</param>
        /// <param name="extraSymbolTable">Additional symbols to bind UDF.</param>
        internal void AddUserDefinedFunction(string script, CultureInfo parseCulture = null, ReadOnlySymbolTable symbolTable = null, ReadOnlySymbolTable extraSymbolTable = null)
        {
            // Phase 1: Side affects are not allowed.
            var options = new ParserOptions() 
            { 
                AllowsSideEffects = false,
                Culture = parseCulture ?? CultureInfo.InvariantCulture 
            };
            var sb = new StringBuilder();

            UserDefinitions.ProcessUserDefinitions(script, options, out var userDefinitionResult, Features.PowerFxV1);

            if (userDefinitionResult.HasErrors)
            {
                sb.AppendLine("Something went wrong when parsing user defined functions.");

                foreach (var error in userDefinitionResult.Errors)
                {
                    error.FormatCore(sb);
                }

                throw new InvalidOperationException(sb.ToString());
            }

            // Compose will handle null symbols
            var composedSymbols = Compose(this, symbolTable, extraSymbolTable);

            foreach (var udf in userDefinitionResult.UDFs)
            {
                AddFunction(udf);
                var binding = udf.BindBody(composedSymbols, new Glue2DocumentBinderGlue(), BindingConfig.Default, features: Features.PowerFxV1);

                List<TexlError> errors = new List<TexlError>();

                if (binding.ErrorContainer.GetErrors(ref errors))
                {
                    sb.AppendLine(string.Join(", ", errors.Select(err => err.ToString())));
                }
            }

            if (sb.Length > 0)
            {
                throw new InvalidOperationException(sb.ToString());
            }
        }

        /// <summary>
        /// Remove variable, entity or constant of a given name. 
        /// </summary>
        /// <param name="name">display or logical name for the variable or entity to be removed. Logical name of constant to be removed.</param>
        public void RemoveVariable(string name)
        {
            using var guard = _guard.Enter(); // Region is single threaded.

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
            using var guard = _guard.Enter(); // Region is single threaded.
            Inc();

            _functions.RemoveAll(name);
        }

        internal void RemoveFunction(TexlFunction function)
        {
            using var guard = _guard.Enter(); // Region is single threaded.
            Inc();

            _functions.RemoveAll(function);
        }

        internal void AddFunctions(TexlFunctionSet functions)
        {
            using var guard = _guard.Enter(); // Region is single threaded.
            Inc();

            if (functions._count == 0)
            {
                return;
            }

            _functions.Add(functions);

            // Add any associated enums 
            EnumStoreBuilder?.WithRequiredEnums(functions);
        }

        internal void AddFunction(TexlFunction function)
        {
            using var guard = _guard.Enter(); // Region is single threaded.
            Inc();
            _functions.Add(function);

            // Add any associated enums 
            EnumStoreBuilder?.WithRequiredEnums(new TexlFunctionSet(function));
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
                nameInfo = new NameLookupInfo(BindKind.OptionSet, optionSet.Type, DPath.Root, 0, optionSet, displayName);
            }
            else if (entity is IExternalDataSource)
            {
                nameInfo = new NameLookupInfo(BindKind.Data, entity.Type, DPath.Root, 0, entity, displayName);
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

        // Sync version for convenience. 
        public void AddHostObject(string name, FormulaType type, Func<IServiceProvider, FormulaValue> getValue)
        {
            this.AddHostObject(name, type, (sp) => Task.FromResult(getValue(sp)));
        }

        /// <summary>
        /// Adds a host object schema, that can be referenced in the formula.
        /// Actual object is added in Runtime config service provider.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="type">Type of the object.</param>
        /// <param name="getValue">Call back that will retrieve object from the service provider.
        /// It can throw CustomFunctionErrorException, that fx will convert to an error.</param>
        public void AddHostObject(string name, FormulaType type, Func<IServiceProvider, Task<FormulaValue>> getValue)
        {
            using var guard = _guard.Enter(); // Region is single threaded.
            var hostDName = ValidateName(name);

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(hostDName, default);
            }

            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data: getValue,
                displayName: default);

            _variables.Add(hostDName, info);
        }
    }
}
