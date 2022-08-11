// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    internal interface IPowerFxScopeExtended : IPowerFxScope
    {
        /// <summary>
        /// Check for errors in the given expression, with parser options. 
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <param name="options">Parser options to use.</param>
        /// <returns>Validation result.</returns>
        CheckResult Check(string expression, ParserOptions options);

        /// <summary>
        /// Provide intellisense for expression, from CheckResult.
        /// </summary>
        IIntellisenseResult Suggest(string expression, CheckResult checkResult, int cursorPosition);
    }
}
