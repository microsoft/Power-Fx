// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Provide an interface to the RecalcEngine.
    /// </summary>
    /// <note>
    /// This is the minimal set of functionality required of a RecalcEngine. Implementations may also provide other public methods, fields, etc.
    /// </note>
    public interface IPowerFxEngine
    {
        /// <summary>
        /// Checks that the provided expression is valid. This means that it is syntactically value and that all types referenced in the epxression are defined in the parameterType.
        /// </summary>
        /// <param name="expressionText">the string representation of the expression to be checked.</param>
        /// <param name="parameterType">the (composite) type definition required to validate the expression.</param>
        /// <param name="options">the parser options to use when validating the expression.</param>
        /// <param name="cultureInfo">culture to use.</param>
        /// <returns></returns>
        CheckResult Check(string expressionText, RecordType parameterType, ParserOptions options = null, CultureInfo cultureInfo = null);
    }
}
