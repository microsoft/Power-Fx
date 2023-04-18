// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// A parsed expression.
    /// </summary>
    /// <returns></returns>
    public interface IExpression
    {
        [Obsolete("Use CheckResult.GetEvaluator() instead.")]
        Task<FormulaValue> EvalAsync(RecordValue parameters, CancellationToken cancellationToken);
    }
}
