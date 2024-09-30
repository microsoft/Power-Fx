// <copyright file="AssertFunction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Functions
{
    /// <summary>
    /// Assert Function.
    /// </summary>
    internal class AssertFunction : ReflectionFunction
    {
        public AssertFunction()
            : base("Assert", FormulaType.Void, new[] { FormulaType.Boolean, FormulaType.String })
        {
            ConfigType = typeof(IReplOutput);            
        }

        public async Task<VoidValue> Execute(IReplOutput output, BooleanValue test, StringValue message, CancellationToken cancel)
        {            
            if (test.Value)
            {
                await output.WriteLineAsync($"PASSED: {message.Value}", OutputKind.Notify, cancel)
                .ConfigureAwait(false);
            } 
            else
            {
                await output.WriteLineAsync($"FAILED: {message.Value}", OutputKind.Error, cancel)
                .ConfigureAwait(false);
            }

            return FormulaValue.NewVoid();
        }
    }
}
