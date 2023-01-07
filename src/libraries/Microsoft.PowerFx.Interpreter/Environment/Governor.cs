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
    /// These virtuals are called by the evaluator, and exceptions thrown by them will abort the evaluation. 
    /// This is a service passed to <see cref="RuntimeConfig"/>, and a derived class can override and throw their own host exceptions if hardware limits are exceeded.
    /// </summary>
    public class Governor
    {
        /// <summary>
        /// Called by evaluator before allocating large memory blocks 
        /// to ensure there's storage to proceed. 
        /// This allows failure before we start to eval a memory intensive operation. 
        /// </summary>
        /// <param name="allocateBytes">Predicted number of bytes this function may need in order to execute. </param>
        public virtual void PollMemory(long allocateBytes)
        {
        }

        /// <summary>
        /// Called throughout engine. This can monitor system resources (like memory) 
        /// and throw an exception to abort execution. 
        /// </summary>
        public virtual void Poll()
        {
        }
    }
}
