// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Functions
{
    /// <summary>
    /// Exit() - requests an Exit from the REPL.
    /// </summary>
    internal class ExitFunction : ReflectionFunction
    {
        private readonly PowerFxREPL _repl;

        public ExitFunction(PowerFxREPL repl)
        {
            _repl = repl;
        }

        public async Task<BooleanValue> Execute(CancellationToken cancel)
        {
            _repl.ExitRequested = true;

            return FormulaValue.New(true);
        }
    }
}
