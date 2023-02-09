// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // A Texl function capable of async invokes. 
    internal interface IAsyncTexlFunction
    {
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken);
    }
}
