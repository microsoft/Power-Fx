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
        /// <param name="expression"></param>
        /// <returns></returns>
        CheckResult Check(string expression);

        /// <summary>
        /// Check for errors in the given expression with parser options. 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        CheckResult Check(string expression, ParserOptions options);

        /// <summary>
        /// Provide intellisense for expression.
        /// </summary>
        IIntellisenseResult Suggest(string expression, int cursorPosition);

        /// <summary>
        /// Provide intellisense for expression with parser options.
        /// </summary>
        IIntellisenseResult Suggest(string expression, int cursorPosition, ParserOptions options);

        /// <summary>
        /// Converts punctuators and identifiers in an expression to the appropriate display format.
        /// </summary>
        string ConvertToDisplay(string expression);
    }
}
