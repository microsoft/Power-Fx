// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// Invoker to execute a function via the interpreter. 
    /// </summary>
    public interface IFunctionInvoker
    {
        Task<FormulaValue> InvokeAsync(FunctionInvokeInfo invokeInfo, CancellationToken cancellationToken);
    }
}
