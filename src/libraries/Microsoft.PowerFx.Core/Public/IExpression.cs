// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        public Task<FormulaValue> EvalAsync(RecordValue parameters, CancellationToken cancel);
    }

    /// <summary>
    /// Extensions for <see cref="IExpression"/>.
    /// </summary>
    public static class IExpressionExtensions
    {
        /// <summary>
        /// Evaluate the expression with a given set of record values.
        /// </summary>
        public static FormulaValue Eval(this IExpression expr, RecordValue parameters, CancellationToken cancel)
        {
            return expr.EvalAsync(parameters, cancel).Result;
        }

        public static FormulaValue Eval(this IExpression expr, RecordValue parameters)
        {
            return expr.Eval(parameters, CancellationToken.None);
        }
    }
}
