// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class PowerFxVersionFunction : BuiltinFunction
    {
        public PowerFxVersionFunction() 
            : base("PowerFxVersion", (l) => "Power Fx Version", FunctionCategories.Information, DType.String, 0, 0, 0)
        {
        }

        public override bool IsSelfContained => true;               

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
