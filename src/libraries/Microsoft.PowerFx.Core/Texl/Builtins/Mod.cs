// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    // Mod(number:n, divisor:n)
    internal sealed class ModFunction : MathTwoArgFunction
    {
        public ModFunction()
            : base("Mod", TexlStrings.AboutMod, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield(new[] { TexlStrings.ModFuncArg1, TexlStrings.ModFuncArg2 });
        }
    }

    // Mod(number:n|*[n], divisor:n|*[n])
    internal sealed class ModTFunction : MathTwoArgTableFunction
    {
        public override bool InConsistentTableResultFixedName => true;

        public ModTFunction()
            : base("Mod", TexlStrings.AboutModT, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ModTFuncArg1, TexlStrings.ModTFuncArg2 };
        }
    }
}
