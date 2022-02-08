// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

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
