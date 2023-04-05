// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Power(number:n, number:n):n
    // Equivalent DAX function: Power
    internal sealed class PowerFunction : MathTwoArgFunction
    {
        public PowerFunction()
            : base("Power", TexlStrings.AboutPower, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PowerFuncArg1, TexlStrings.PowerFuncArg2 };
        }
    }

    // Power(number:n|*[n], number:n|*[n]):*[n]
    // Equivalent DAX function: Power
    internal sealed class PowerTFunction : MathTwoArgTableFunction
    {
        public override bool NonConsistentUseSecondArg => true;

        public PowerTFunction()
            : base("Power", TexlStrings.AboutPowerT, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PowerTFuncArg1, TexlStrings.PowerTFuncArg2 };
        }
    }
}
