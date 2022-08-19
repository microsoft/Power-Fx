// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
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
        /// Enable a Collect() function which allows scripts to append table records.
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public static void EnableCollectFunction(this PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(new CollectFunction());
        }
    }
}
