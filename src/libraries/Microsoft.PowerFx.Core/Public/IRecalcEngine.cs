// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Provide an interface to the RecalcEngine.
    /// </summary>
    /// <note>
    /// This is the minimal set of functionality required of a RecalcEngine. Implementations may also provide other public methods, fields, etc.
    /// </note>
    public interface IRecalcEngine
    {
        /// <summary>
        /// Checks that the provided expression is valid. This means that it is syntactically value and that all types referenced in the epxression are defined in the parameterType.
        /// </summary>
        /// <param name="expressionText">the string representation of the expression to be checked.</param>
        /// <param name="parameterType">the (composite) type definition required to validate the expression.</param>
        /// <returns></returns>
        CheckResult Check(string expressionText, FormulaType parameterType);
    }
}
