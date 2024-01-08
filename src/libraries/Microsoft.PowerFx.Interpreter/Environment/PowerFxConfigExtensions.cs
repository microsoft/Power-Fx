// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;

namespace Microsoft.PowerFx
{
    public static class PowerFxConfigExtensions
    {
        public static void AddFunction(this PowerFxConfig powerFxConfig, ReflectionFunction function)
        {
            powerFxConfig.AddFunction(function.GetTexlFunction());
        }

        public static void AddFunction(this SymbolTable symbolTable, ReflectionFunction function)
        {
            symbolTable.AddFunction(function.GetTexlFunction());
        }

        /// <summary>
        /// Enable a Set() function which allows scripts to do <see cref="RecalcEngine.UpdateVariable(string, Types.FormulaValue)"/>.
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public static void EnableSetFunction(this PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(new RecalcEngineSetFunction());
        }

        private static IEnumerable<TexlFunction> MutationFunctionsList()
        {
            return new TexlFunction[]
            {
                new CollectImpl(),
                new CollectScalarImpl(),
                new RecalcEngineSetFunction(),
                new PatchFunction(),
                new RemoveFunction(),
                new ClearFunction(),
                new ClearCollectFunction(),
            };
        }

        /// <summary>
        /// Enable all multation functions which allows scripts to execute side effect behavior.
        /// This will add the functions to the <see cref="SymbolTable"/>.
        /// </summary>
        /// <param name="symbolTable"></param>
        public static void EnableMutationFunctions(this SymbolTable symbolTable)
        {
            foreach (var function in MutationFunctionsList())
            {
                symbolTable.AddFunction(function);
            }
        }

        /// <summary>
        /// Enable all multation functions which allows scripts to execute side effect behavior.
        /// This will add the functions to the <see cref="PowerFxConfig.SymbolTable"/>.
        /// </summary>
        /// <param name="config"></param>
        public static void EnableMutationFunctions(this PowerFxConfig config)
        {
            foreach (var function in MutationFunctionsList())
            {
                config.SymbolTable.AddFunction(function);
            }
        }

        [Obsolete("RegEx is still in preview. Grammar may change.")]
        public static void EnableRegExFunctions(this PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IAsyncTexlFunction> func in Library.RegexFunctions(regExTimeout, regexTypeCache))
            {
                if (config.SymbolTable.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.SymbolTable.AddFunction(func.Key);
                config.AdditionalFunctions.Add(func.Key, func.Value);
            }
        }
    }
}
