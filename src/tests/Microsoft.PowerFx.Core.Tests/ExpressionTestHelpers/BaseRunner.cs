﻿// Copyright (c) Microsoft Corporation.
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
        public abstract Task<FormulaValue> RunAsync(string expr);


        // Return false if setup handler isn't defined for this runner
        public abstract bool TryDoSetup(string setupHandlerName);

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
