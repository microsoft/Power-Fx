// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IAsyncTexlFunctionService
    {
        Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken);
    }
}
