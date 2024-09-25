// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // A Texl function capable of async invokes, using TimeZoneInfo and IRContext. 
    internal interface IAsyncTexlFunction4
    {
        Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken);
    }
}
