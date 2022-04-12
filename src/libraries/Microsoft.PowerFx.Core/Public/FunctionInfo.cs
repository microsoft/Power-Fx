// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Information about a built-in function.
    /// </summary>
    public class FunctionInfo
    {
        private readonly TexlFunction _fnc;

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
    }
}
