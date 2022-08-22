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
    /// Used between Symbol Table resolver and IR. Enables describing constants or 
    /// globals that are the same across evals. 
    /// Object provided by resolver data in <see cref="NameLookupInfo.Data"/>.
    /// IR then recognizes this and will look these values and will retrieve the Value at runtime. 
    /// </summary>
    internal interface ICanGetValue
    {
        FormulaValue Value { get; }
    }

    /// <summary>
    /// Used between Symbol Table resolver and IR.
    /// NameInfo data to associate a symbol at bind time with a runtime config at runtime. 
    /// Object provided by resolver data in <see cref="NameLookupInfo.Data"/>.
    /// IR then recognizes this and will look these values up via ReadOnlySymbolValues.TryGetValue.
    /// </summary>
    internal class NameSymbol
    {
        public NameSymbol(string name)
        {
            Name = name;
        }
        
        public string Name { get; private set; }
    }

    /// <summary>
    /// Provides symbols to the engine. This includes variables (locals, globals), enums, options sets, and functions.
    /// SymbolTables are mutable to support sessionful scenarios and can abe chained together. 
    /// This is a publicly facing class around a <see cref="INameResolver"/>.
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    public class SymbolTable : ReadOnlySymbolTable
    {
        // Expose public setters
        public new ReadOnlySymbolTable Parent
        {
            get => _parent;
            init
            {
                Inc();
                _parent = value;
            }
        }

        private void ValidateName(string name)
        {
            if (!DName.IsValidDName(name))
            {
                throw new ArgumentException("Invalid name: ${name}");
            }
        }

        /// <summary>
        /// Provide variable for binding only.
        /// Value must be provided at runtime.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public void AddVariable(string name, FormulaType type)
        {
            Inc();
            ValidateName(name);

            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data: new NameSymbol(name));

            _variables.Add(name, info); // can't exist
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

            _variables.Add(name, info); // can't exist
        }

        /// <summary>
        /// Remove variable of given name. 
        /// </summary>
        /// <param name="name"></param>
        public void RemoveVariable(string name)
        {
            Inc();

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

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            // For entities without a display name, add (logical, logical) pair to still be included in collision checks.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(entity.EntityName, displayName != default ? displayName : entity.EntityName);
            }

            _environmentSymbols.Add(entity.EntityName, entity);
        }
    }
}
