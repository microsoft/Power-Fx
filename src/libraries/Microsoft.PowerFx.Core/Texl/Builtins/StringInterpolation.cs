// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // StringInterpolation(source1:s, source2:s, ...)
    // No DAX function, compiler-only, not available for end users, no table support
    // String interpolations such as $"Hello {"World"}" translate into a call to this function
    internal sealed class StringInterpolationFunction : ConcatenateFunctionBase
    {
        public StringInterpolationFunction()
            : base("StringInterpolation")
        {
        }
    }
}
