// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core.Tests
{
    // Base class for running a lightweght test. 
    public abstract class BaseRunner
    {
        /// <summary>
        /// Runs a PowerFx test case, with optional setup.
        /// </summary>
        /// <param name="expr">PowerFx expression.</param>
        /// <param name="setupHandlerName">Optional name of a setup handler to run. Throws SetupHandlerNotImplemented if not found.</param>
        /// <returns>Result of evaluating Expr.</returns>
        public abstract Task<FormulaValue> RunAsync(string expr, string setupHandlerName = null);

        // Get the friendly name of the harness. 
        public virtual string GetName()
        {
            return GetType().Name;
        }

        public virtual bool IsError(FormulaValue value)
        {
            return value is ErrorValue;
        }
    }
}
