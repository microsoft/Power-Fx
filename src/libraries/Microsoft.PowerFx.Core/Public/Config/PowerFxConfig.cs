// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
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
        internal static readonly int DefaultMaximumExpressionLength = 1000;

        /// <summary>
        /// Global symbols. Additional symbols beyond default function set. 
        /// </summary>
        public SymbolTable SymbolTable { get; set; } = new SymbolTable
        {
            DebugName = "DefaultConfig"
        };
        
        internal IDictionary<TexlFunction, object /*AsyncFunctionPtr*/> ConfigDependentFunctions = new Dictionary<TexlFunction, object>();

        [Obsolete("Use Config.EnumStore or symboltable directly")]
        internal EnumStoreBuilder EnumStoreBuilder => SymbolTable.EnumStoreBuilder;

        internal IEnumStore EnumStore => ReadOnlySymbolTable.Compose(SymbolTable);        

        public Features Features { get; }

        public int MaxCallDepth { get; set; }

        public int MaximumExpressionLength { get; set; }

        private PowerFxConfig(EnumStoreBuilder enumStoreBuilder, Features features = null)
        {
            Features = features ?? Features.None;
            SymbolTable.EnumStoreBuilder = enumStoreBuilder;
            MaxCallDepth = DefaultMaxCallDepth;
            MaximumExpressionLength = DefaultMaximumExpressionLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConfig"/> class.        
        /// </summary>          
        public PowerFxConfig()
            : this(Features.None)
        {
        }

        /// <summary>
        /// Information about available functions.
        /// </summary>
        [Obsolete("Migrate to SymbolTables")]
        public IEnumerable<FunctionInfo> FunctionInfos =>
            new Engine(this).SupportedFunctions.Functions.Functions
            .Concat(SymbolTable.Functions.Functions)
            .Select(f => new FunctionInfo(f));

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConfig"/> class.
        /// </summary>        
        /// <param name="features">Features to use.</param>
        public PowerFxConfig(Features features)
            : this(new EnumStoreBuilder().WithRequiredEnums(BuiltinFunctionsCore._library), features)
        {
        }

        /// <summary>
        /// Stopgap until Enum Store is refactored. Do not rely on, this will be removed. 
        /// </summary>
        internal static PowerFxConfig BuildWithEnumStore(EnumStoreBuilder enumStoreBuilder)
        {
            return BuildWithEnumStore(enumStoreBuilder, Features.None);
        }

        internal static PowerFxConfig BuildWithEnumStore(EnumStoreBuilder enumStoreBuilder, Features features)
        {
            return BuildWithEnumStore(enumStoreBuilder, BuiltinFunctionsCore._library, features: features);
        }

        internal static PowerFxConfig BuildWithEnumStore(EnumStoreBuilder enumStoreBuilder, TexlFunctionSet coreFunctions)
        {
            return BuildWithEnumStore(enumStoreBuilder, coreFunctions, Features.None);
        }

        internal static PowerFxConfig BuildWithEnumStore(EnumStoreBuilder enumStoreBuilder, TexlFunctionSet coreFunctions, Features features)
        {
            var config = new PowerFxConfig(enumStoreBuilder, features);

            config.AddFunctions(coreFunctions);

            return config;
        }

        // For PAClient cases - JSRunner. These don't derive from Engine. 
        [Obsolete("Migrate to SymbolTables")]
        internal void SetCoreFunctions(IEnumerable<TexlFunction> functions)
        {
            foreach (var func in functions)
            {
                AddFunction(func);
            }
        }

        internal bool GetSymbols(string name, out NameLookupInfo symbol) => SymbolTable._variables.TryGetValue(name, out symbol);

        internal IEnumerable<string> GetSuggestableSymbolName() => SymbolTable._variables.Keys;

        internal void AddEntity(IExternalEntity entity, DName displayName = default)
            => SymbolTable.AddEntity(entity, displayName);

        internal void AddFunction(TexlFunction function)
        {
            if (function.HasLambdas || function.HasColumnIdentifiers)
            {
                // We limit to 20 arguments as MaxArity could be set to int.MaxValue 
                // and checking up to 20 arguments is enough for this validation
                for (var i = 0; i < Math.Min(function.MaxArity, 20); i++)
                {
                    if (function.HasLambdas && function.HasColumnIdentifiers && function.IsLambdaParam(i) && function.IsIdentifierParam(i))
                    {
                        (var message, var _) = ErrorUtils.GetLocalizedErrorContent(TexlStrings.ErrInvalidFunction, null, out var errorResource);
                        throw new ArgumentException(message);
                    }
                }

                var overloads = SymbolTable.Functions.WithName(function.Name).Where(tf => tf.HasLambdas || tf.HasColumnIdentifiers);

                if (overloads.Any())
                {
                    for (var i = 0; i < Math.Min(function.MaxArity, 20); i++)
                    {
                        if ((function.IsLambdaParam(i) && overloads.Any(ov => ov.HasColumnIdentifiers && ov.IsIdentifierParam(i))) ||
                            (function.IsIdentifierParam(i) && overloads.Any(ov => ov.HasLambdas && ov.IsLambdaParam(i))))
                        {
                            (var message, var _) = ErrorUtils.GetLocalizedErrorContent(TexlStrings.ErrInvalidFunction, null, out var errorResource);
                            throw new ArgumentException(message);
                        }
                    }
                }
            }

            SymbolTable.AddFunction(function);
        }

        internal void AddFunctions(TexlFunctionSet functionSet)
        {
            SymbolTable.AddFunctions(functionSet);
        }

        public void AddOptionSet(OptionSet optionSet, DName optionalDisplayName = default)
        {
            AddEntity(optionSet, optionalDisplayName);
        }

        internal bool TryGetVariable(DName name, out DName displayName)
            => SymbolTable.TryGetVariable(name, out _, out displayName);
    }
}
