// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using static Microsoft.PowerFx.Functions.Library;

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
        /// Enables Match/IsMatch/MatchAll functions.
        /// </summary>
        /// <param name="powerFxConfig">Power Fx configuration.</param>
        /// <param name="regExTimeout">Timeout duration for regular expression execution. 0 will default to 1 second.</param>
        /// <param name="regexCacheSize">Size of the regular expression cache. -1 = disabled. 0 = infinite.</param>
        public static void EnableRegExFunctions(this PowerFxConfig powerFxConfig, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {            
            foreach (var function in Library.EnableRegexFunctions(regExTimeout, regexCacheSize))
            {
                powerFxConfig.AddFunction(function);
            }
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
    }
}
