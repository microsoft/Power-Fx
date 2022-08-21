// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// A container that allows for compiler customization.
    /// </summary>
    public sealed class PowerFxConfig
    {
        internal static readonly int DefaultMaxCallDepth = 20;

        /// <summary>
        /// Global symbols. Additional symbols beyond default function set. 
        /// </summary>
        public SymbolTable SymbolTable { get; set; } = new SymbolTable
        {
            DebugName = "DefaultConfig"
        };
                
        internal EnumStoreBuilder EnumStoreBuilder => SymbolTable.EnumStoreBuilder;

        public CultureInfo CultureInfo { get; }

        public Features Features { get; }

        public int MaxCallDepth { get; set; }

        private PowerFxConfig(CultureInfo cultureInfo, EnumStoreBuilder enumStoreBuilder, Features features = Features.None)
        {
            CultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            Features = features;            
            SymbolTable.EnumStoreBuilder = enumStoreBuilder;
            MaxCallDepth = DefaultMaxCallDepth;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConfig"/> class.        
        /// </summary>
        /// <param name="cultureInfo">Culture to use.</param>      
        public PowerFxConfig(CultureInfo cultureInfo = null)
            : this(cultureInfo, Features.None)
        {
        }

        /// <summary>
        /// Information about available functions.
        /// </summary>
        [Obsolete("Migrate to SymbolTables")]
        public IEnumerable<FunctionInfo> FunctionInfos => 
            new Engine(this).SupportedFunctions.Functions
            .Concat(SymbolTable.Functions)
            .Select(f => new FunctionInfo(f));

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConfig"/> class.
        /// </summary>
        /// <param name="cultureInfo">Culture to use.</param>
        /// <param name="features">Features to use.</param>
        public PowerFxConfig(CultureInfo cultureInfo, Features features)
            : this(cultureInfo, new EnumStoreBuilder().WithRequiredEnums(BuiltinFunctionsCore.BuiltinFunctionsLibrary), features)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConfig"/> class.
        /// </summary>
        /// <param name="features">Features to use.</param>
        public PowerFxConfig(Features features)
            : this(null, features)
        {
        }

        /// <summary>
        /// Stopgap until Enum Store is refactored. Do not rely on, this will be removed. 
        /// </summary>
        internal static PowerFxConfig BuildWithEnumStore(CultureInfo cultureInfo, EnumStoreBuilder enumStoreBuilder)
        {
            return BuildWithEnumStore(cultureInfo, enumStoreBuilder, Core.Texl.BuiltinFunctionsCore.BuiltinFunctionsLibrary);
        }

        internal static PowerFxConfig BuildWithEnumStore(CultureInfo cultureInfo, EnumStoreBuilder enumStoreBuilder, IEnumerable<TexlFunction> coreFunctions)
        {
            var config = new PowerFxConfig(cultureInfo, enumStoreBuilder);

            foreach (var func in coreFunctions)
            {
                config.AddFunction(func);
            }

            return config;
        }

        // For PAClient cases - JSRunner,Dataverse. These don't derive from Engine. 
        [Obsolete("Migrate to SymbolTables")]
        internal void SetCoreFunctions(IEnumerable<TexlFunction> functions)
        {
            foreach (var func in functions)
            {
                AddFunction(func);
            }
        }

        internal IEnumerable<IExternalEntity> GetSymbols() => SymbolTable._environmentSymbols.Values;

        internal string GetSuggestableSymbolName(IExternalEntity entity)
            => SymbolTable.GetSuggestableSymbolName(entity);

        internal void AddEntity(IExternalEntity entity, DName displayName = default)
            => SymbolTable.AddEntity(entity, displayName);

        internal void AddFunction(TexlFunction function)
        {
            var comparer = new TexlFunctionComparer();
            if (!SymbolTable.Functions.Contains(function, comparer))
            {
                SymbolTable.AddFunction(function);
            }
            else
            {
                throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
            }
        }

        public void AddOptionSet(OptionSet optionSet, DName optionalDisplayName = default)
        {
            AddEntity(optionSet, optionalDisplayName);
        }

        internal bool TryGetSymbol(DName name, out IExternalEntity symbol, out DName displayName)
            => SymbolTable.TryGetSymbol(name, out symbol, out displayName);
    }
}
