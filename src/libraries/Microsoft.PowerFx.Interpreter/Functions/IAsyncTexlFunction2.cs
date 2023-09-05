// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // A Texl function capable of async invokes. 
    [Obsolete("This interface is obsolete and will be removed in a future release. Please use IAsyncTexlFunction3 instead.")]
    internal interface IAsyncTexlFunction2
    {
        Task<FormulaValue> InvokeAsync(FormattingInfo context, FormulaValue[] args, CancellationToken cancellationToken);
    }
}
