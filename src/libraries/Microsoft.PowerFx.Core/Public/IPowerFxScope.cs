// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Provide intellisense (Design-time) support. 
    /// </summary>
    public interface IPowerFxScope
    {
        /// <summary>
        /// Check for errors in the given expression. 
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <returns>The result of the validation as a <see cref="CheckResult"/>.</returns>
        CheckResult Check(string expression);

        /// <summary>
        /// Provide intellisense suggestions for the given expression at the specified cursor position.
        /// </summary>
        /// <param name="expression">The expression for which to provide suggestions.</param>
        /// <param name="cursorPosition">The position of the cursor within the expression.</param>
        /// <returns>The intellisense result containing suggestions.</returns>
        IIntellisenseResult Suggest(string expression, int cursorPosition);

        /// <summary>
        /// Converts punctuators and identifiers in an expression to the appropriate display format.
        /// </summary>
        /// <param name="expression">The expression to convert for display.</param>
        /// <returns>The expression with display formatting applied.</returns>
        string ConvertToDisplay(string expression);
    }

    internal interface IPowerFxScopeV2 : IPowerFxScope
    {
        IEnumerable<ExpressionError> GetErrors(string expression);
    }
}
