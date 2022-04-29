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
        /// Provide intellisense for expression.
        /// </summary>
        IIntellisenseResult Suggest(string expression, int cursorPosition);
    }
}
