// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// A parsed expression.
    /// </summary>
    /// <returns></returns>
    public interface IExpression
    {
        /// <summary>
        /// Evaluate the expression with a given set of record values.
        /// </summary>
        public FormulaValue Eval(RecordValue parameters);
    }
}
