// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // A Texl function capable of async invokes.     
    internal interface IAsyncTexlFunction3
    {
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
