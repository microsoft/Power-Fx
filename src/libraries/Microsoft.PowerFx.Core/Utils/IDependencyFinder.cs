// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// Exposes the ability parse a formula, walk the tree and return a list of dependencies
    /// or variable names as strings.
    /// </summary>
    internal interface IDependencyFinder
    {
        /// <summary>
        /// Given a formula, create a list of dependencies or variable names as a
        /// set of strings.
        /// </summary>
        HashSet<string> FindDependencies(FormulaWithParameters formulaWithParameters);
    }
}
