// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        /// <returns>Validation result.</returns>
        CheckResult Check(string expression);

        /// <summary>
        /// Check for errors in the given expression, with parser options.
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <param name="options">Parser options to use. Null is valid.</param>
        /// <returns>Validation result.</returns>
        CheckResult Check(string expression, ParserOptions options);

        /// <summary>
        /// Provide intellisense for expression.
        /// </summary>
        IIntellisenseResult Suggest(string expression, int cursorPosition);

        /// <summary>
        /// Provide intellisense for expression, with parser options.
        /// </summary>
        IIntellisenseResult Suggest(string expression, int cursorPosition, ParserOptions options);

        /// <summary>
        /// Converts punctuators and identifiers in an expression to the appropriate display format.
        /// </summary>
        string ConvertToDisplay(string expression);
    }
}
