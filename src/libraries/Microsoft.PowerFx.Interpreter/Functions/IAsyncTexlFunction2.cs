// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // A Texl function capable of async invokes. 
    internal interface IAsyncTexlFunction2
    {
        Task<FormulaValue> InvokeAsync(FormattingInfo context, FormulaValue[] args, FormulaType returnTypeOverride, CancellationToken cancellationToken);
    }
}
