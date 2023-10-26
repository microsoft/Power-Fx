// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx.Core.Texl
{
    internal class ControlFlowFunctionHelper
    {
        /// <summary>
        /// Returns a list of argument indices that correspond to the lambda expressions in
        /// the <see cref="IfFunction"/> and <see cref="SwitchFunction"/>.
        /// </summary>
        /// <param name="argCount"></param>
        /// <returns></returns>
        internal static IEnumerable<int> GetDataSourceArgumentIndices(int argCount)
        {
            if (argCount > 2)
            {
                for (var i = 1; i < argCount;)
                {
                    yield return i;

                    // If there are an odd number of args, the last arg also participates.
                    i += 2;
                    if (i == argCount)
                    {
                        i--;
                    }
                }
            }
        }
    }
}
