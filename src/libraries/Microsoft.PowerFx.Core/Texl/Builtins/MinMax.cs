// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(arg1:n, arg2:n, ..., argN:n)
    // Max(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel functions: Min, Max
    internal sealed class MinFunction : MathFunction
    {
        public override bool HasPreciseErrors => true;

        public MinFunction()
            : base("Min", TexlStrings.AboutMin, FunctionCategories.MathAndStat, 1, int.MaxValue, replaceBlankWithZero: false, nativeDecimal: true, nativeDateTime: true)
        {
        }
    }

    internal sealed class MaxFunction : MathFunction
    {
        public override bool HasPreciseErrors => true;

        public MaxFunction()
            : base("Max", TexlStrings.AboutMax, FunctionCategories.MathAndStat, 1, int.MaxValue, replaceBlankWithZero: false, nativeDecimal: true, nativeDateTime: true)
        {
        }
    }
}
