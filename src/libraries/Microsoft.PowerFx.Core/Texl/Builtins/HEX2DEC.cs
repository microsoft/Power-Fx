// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Hex2Dec(number:n, [places:n])
    internal sealed class Hex2DecFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool HasPreciseErrors => true;

        public Hex2DecFunction()
            : base("Hex2Dec", TexlStrings.AboutHex2Dec, FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Hex2DecArg1 };
        }
    }

    // Hex2DecT(number:[n], [places:n])
    internal sealed class Hex2DecTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public Hex2DecTFunction()
            : base("Hex2Dec", TexlStrings.AboutHex2DecT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Hex2DecTArg1 };
        }
    }
}