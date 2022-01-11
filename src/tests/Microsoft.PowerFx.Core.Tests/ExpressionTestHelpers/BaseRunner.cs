// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public abstract Task<FormulaValue> RunAsync(string expr);

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
