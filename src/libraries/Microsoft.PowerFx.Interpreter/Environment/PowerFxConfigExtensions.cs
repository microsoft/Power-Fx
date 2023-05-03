// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Public.Config;
using Microsoft.PowerFx.Core.Types;
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

        /// <summary>
        /// Enable all multation functions which allows scripts to execute side effect behavior.
        /// </summary>
        /// <param name="symbolTable"></param>
        public static void EnableMutationFunctions(this SymbolTable symbolTable)
        {
            symbolTable.AddFunction(new RecalcEngineSetFunction());
            symbolTable.AddFunction(new CollectFunction());
            symbolTable.AddFunction(new PatchFunction());
            symbolTable.AddFunction(new RemoveFunction());
            symbolTable.AddFunction(new ClearFunction());
            symbolTable.AddFunction(new ClearCollectFunction());
        }

        public static void EnableRegExFunctions(this PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {
            ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>> regexTypeCache = regexCacheSize == -1 ? null : new ConcurrentDictionary<string, Tuple<DType, bool, bool, bool>>();

            foreach ((TexlFunction function, Action<IBasicServiceProvider> addFunctionImpl) in Library.RegexFunctions(regExTimeout, regexTypeCache, regexCacheSize))
            {
                if (config.SymbolTable.Functions.AnyWithName(function.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }
                
                config.SymbolTable.AddFunction(function);
                config.AddFunctionImplementations.Add(addFunctionImpl);
            }
        }
    }
}
