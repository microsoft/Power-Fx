// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // HEX2DEC(number:n, [places:n])
    internal sealed class HEX2DECFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool HasPreciseErrors => true;

        public HEX2DECFunction()
            : base("HEX2DEC", TexlStrings.AboutHEX2DEC, FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.HEX2DECArg1 };
        }
    }

    // HEX2DECT(number:[n], [places:n])
    internal sealed class HEX2DECTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public HEX2DECTFunction()
            : base("HEX2DECT", TexlStrings.AboutHEX2DECT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.HEX2DECTArg1 };
        }
    }
}
