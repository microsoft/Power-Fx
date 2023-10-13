// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;

namespace Microsoft.PowerFx
{
    public static class PowerFxConfigExtensions
    {
        public static void AddFunction(this PowerFxConfig powerFxConfig, ReflectionFunction function)
        {
            powerFxConfig.AddFunction(function.GetTexlFunction(), function);
        }

        public static void AddFunction(this SymbolTable symbolTable, ReflectionFunction function)
        {
            symbolTable.AddFunction(function.GetTexlFunction(), function);
        }

        /// <summary>
        /// Enable a Set() function which allows scripts to do <see cref="RecalcEngine.UpdateVariable(string, Types.FormulaValue)"/>.
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public static void EnableSetFunction(this PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(new RecalcEngineSetFunction(), null /* No implementation */);
        }

        /// <summary>
        /// Enable all multation functions which allows scripts to execute side effect behavior.
        /// </summary>
        /// <param name="symbolTable"></param>
        public static void EnableMutationFunctions(this SymbolTable symbolTable)
        {
            symbolTable.AddFunction(new RecalcEngineSetFunction(), null /* no implementation */);
            symbolTable.AddFunction(new CollectFunction(), new CollectFunctionImpl());
            symbolTable.AddFunction(new PatchFunction(), new PatchFunctionImpl());
            symbolTable.AddFunction(new RemoveFunction(), new RemoveFunctionImpl());

            ClearFunctionImpl clearFunctionImpl = new ClearFunctionImpl();
            symbolTable.AddFunction(new ClearFunction(), clearFunctionImpl);
            symbolTable.AddFunction(new ClearCollectFunction(), new ClearCollectFunctionImpl(clearFunctionImpl));
        }

        [Obsolete("RegEx is still in preview. Grammar may change.")]
        public static void EnableRegExFunctions(this PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IFunctionImplementation> func in Library.RegexFunctions(regExTimeout, regexTypeCache))
            {
                if (config.SymbolTable.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.SymbolTable.AddFunction(func.Key, func.Value);                
            }
        }
    }
}
