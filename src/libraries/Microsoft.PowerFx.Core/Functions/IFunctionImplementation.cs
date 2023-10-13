// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IFunctionImplementation
    {        
        Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
