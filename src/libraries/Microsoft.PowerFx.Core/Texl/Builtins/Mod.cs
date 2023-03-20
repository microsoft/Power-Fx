// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Mod(number:n, divisor:n)
    internal sealed class ModFunction : MathFunction
    {
        public ModFunction()
            : base("Mod", TexlStrings.AboutMod, FunctionCategories.MathAndStat, 2, 2, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Generic and use Math func names?
            return EnumerableUtils.Yield(new[] { TexlStrings.ModFuncArg1, TexlStrings.ModFuncArg2 });
        }
    }

    // Mod(number:n|*[n], divisor:n|*[n])
    // Decimal TODO: Should derive from MathTableFunction, needs interpreter implementation
    internal sealed class ModTFunction : MathTableFunction
    {
        public ModTFunction()
            : base("Mod", TexlStrings.AboutModT, FunctionCategories.Table, 2, 2, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Generic and use Math func names?
            yield return new[] { TexlStrings.ModTFuncArg1, TexlStrings.ModTFuncArg2 };
        }
    }
}
