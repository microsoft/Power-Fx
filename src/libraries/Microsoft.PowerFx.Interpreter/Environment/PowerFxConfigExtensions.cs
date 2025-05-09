﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

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

        public static void AddEnvironmentVariables(this SymbolValues symbolValues, RecordValue recordValue)
        {
            var variablesRecordValue = FormulaValue.NewRecordFromFields(new NamedValue("Variables", recordValue));
            symbolValues.Add("Environment", variablesRecordValue);
        }

        /// <summary>
        /// Enable a Set() function which allows scripts to do <see cref="RecalcEngine.UpdateVariable(string, Types.FormulaValue, SymbolProperties)"/>.
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
            symbolTable.AddFunction(new PatchImpl());
            symbolTable.AddFunction(new PatchSingleRecordImpl());
            symbolTable.AddFunction(new PatchAggregateImpl());
            symbolTable.AddFunction(new PatchAggregateSingleTableImpl());
            symbolTable.AddFunction(new RemoveFunction());
            symbolTable.AddFunction(new ClearImpl());
            symbolTable.AddFunction(new ClearCollectImpl());
            symbolTable.AddFunction(new ClearCollectScalarImpl());
            symbolTable.AddFunction(new CollectImpl());
            symbolTable.AddFunction(new CollectScalarImpl());
        }

        [Obsolete("FileFunctions are still in preview.")]
        public static void EnableFileFunctions(this SymbolTable symbolTable)
        {
            symbolTable.AddFunction(new FileInfoFunctionImpl());
        }

        public static void EnableRegExFunctions(this PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IAsyncTexlFunction> func in Library.RegexFunctions(regExTimeout, regexTypeCache))
            {
                if (config.ComposedConfigSymbols.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.InternalConfigSymbols.AddFunction(func.Key);
                config.AdditionalFunctions.Add(func.Key, func.Value);
            }
        }

        [Obsolete("Join is still in preview.")]
        public static void EnableJoinFunction(this PowerFxConfig config)
        {
            config.SymbolTable.AddFunction(new JoinImpl());
        }

        [Obsolete("OptionSetInfo function is deprecated. Use the Value function on an option set backed by a number and the Boolean function on an option set backed by a Boolean instead. A new ChoiceInfo function is in the works for access to logical names.")]
        public static void EnableOptionSetInfo(this PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(new OptionSetInfoFunction());
        }
    }
}
