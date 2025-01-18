// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // Texl function interface with IServiceProvider
    // Only product impl is JsonFunctionImpl.
    // Remove this: https://github.com/microsoft/Power-Fx/issues/2818
    internal interface IAsyncTexlFunction5
    {
        Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken);        
    }
}
