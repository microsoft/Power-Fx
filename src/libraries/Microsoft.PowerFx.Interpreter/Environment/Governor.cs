// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Throttling object to allow a host to control limits of memory and hardware resources.
    /// </summary>
    public class Governor
    {
        // Called by evaluator when allocating large memory blocks 
        // to ensure there's storage to proceed. 
        public virtual void PollMemory(long allocateBytes)
        {
        }

        // Called throughout engine. This can monitor system resources (like memory)
        // and throw an exception to abort execution. 
        public virtual void Poll()
        {
        }
    }
}
