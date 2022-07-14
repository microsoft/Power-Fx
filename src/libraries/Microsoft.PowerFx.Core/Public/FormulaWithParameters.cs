// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Encapsulate a formula and the parameters it has available. 
    /// </summary>
    public class FormulaWithParameters
    {
        internal readonly string _expression; // Formula
        internal readonly RecordType _schema; // context formula can access.

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaWithParameters"/> class.
        /// </summary>
        /// <param name="expression">The text version of the expression.</param>
        /// <param name="parameterTypes">The static type of parameters (context) available to this formula. 
        /// If omited, this formula doesn't have any additional parameters. 
        /// </param>
        public FormulaWithParameters(string expression, RecordType parameterTypes = null)
        {
            _expression = expression;
            _schema = parameterTypes ?? RecordType.Empty();
        }
    }
}
