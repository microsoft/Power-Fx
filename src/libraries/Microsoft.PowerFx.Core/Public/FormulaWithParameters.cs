// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Encapsulate a formula and the parameters it has available. 
    /// </summary>
    public class FormulaWithParameters
    {
        internal readonly string _expression; // Formula
        internal readonly FormulaType _schema; // context formula can access.

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression">The text version of the expression.</param>
        /// <param name="parameterTypes">The static type of parameters (context) available to this formula. 
        /// If omited, this formula doesn't have any additional parameters. 
        /// </param>
        public FormulaWithParameters(string expression, FormulaType parameterTypes = null)
        {
            _expression = expression;
            _schema = parameterTypes ?? new RecordType();
        }
    }
}
