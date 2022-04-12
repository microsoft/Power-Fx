// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Information about a built-in function.
    /// </summary>
    [ThreadSafeImmutable]
    public class FunctionInfo
    {
        internal readonly TexlFunction _fnc;

        internal FunctionInfo(TexlFunction fnc)
        {
            _fnc = fnc ?? throw new ArgumentNullException(nameof(fnc));
        }

        /// <summary>
        /// Name of the function.
        /// </summary>
        public string Name => _fnc.Name;

        /// <summary>
        /// Minimal arity of the function.
        /// </summary>
        public int MinArity => _fnc.MinArity;

        /// <summary>
        /// Maximal arity of the function.
        /// </summary>
        public int MaxArity => _fnc.MaxArity;

        /// <summary>
        /// Whether this function has lambdas as parameters.
        /// </summary>
        public bool HasLambdas => _fnc.HasLambdas;

        /// <summary>
        /// Whether the <paramref name="i" />th parameter is a lambda parameter.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool IsLambdaParam(int i) => _fnc.IsLambdaParam(i);

        /// <summary>
        /// Function parameter types.
        /// </summary>
        public IReadOnlyList<FormulaType> ParamTypes => _fnc.ParamTypes.Select(FormulaType.Build).ToList();
    }
}
