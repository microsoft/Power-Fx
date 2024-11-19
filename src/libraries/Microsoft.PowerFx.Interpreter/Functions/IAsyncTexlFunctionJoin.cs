// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    [Obsolete("This interface is temporary and its meant only to support Join function. It will be soon removed.")]
    internal interface IAsyncTexlFunctionJoin
    {
        Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args);
    }
}
