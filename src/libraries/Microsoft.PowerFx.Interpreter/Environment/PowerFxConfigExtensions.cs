// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
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

        private static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromSeconds(1);
        private static bool regexFunctionsAdded = false;

        /// <summary>
        /// Enables Match/IsMatch/MatchAll functions.
        /// </summary>
        /// <param name="powerFxConfig">Power Fx configuration.</param>
        /// <param name="regExTimeout">Timeout duration for regular expression execution. 0 will default to 30 seconds.</param>
        public static void EnableRegExFunctions(this PowerFxConfig powerFxConfig, TimeSpan regExTimeout = default)
        {
            if (regExTimeout == TimeSpan.Zero)
            {
                regExTimeout = DefaultRegexTimeout;
            }

            if (regExTimeout.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regExTimeout), "Timeout duration for regular expression execution must be positive.");
            }

            Library.RegexTimeout = regExTimeout;
            if (!regexFunctionsAdded)
            {
                powerFxConfig.AddFunction(new IsMatchFunction());
                regexFunctionsAdded = true;
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
