// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Dec2Hex(number:n, [places:n])
    internal sealed class Dec2HexFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public Dec2HexFunction()
            : base("Dec2Hex", TexlStrings.AboutDec2Hex, FunctionCategories.MathAndStat, DType.String, 0, 1, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Dec2HexArg1, TexlStrings.Dec2HexArg2 };
        }
    }

    // Dec2HexT(number:[n], [places:n])
    internal sealed class Dec2HexTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public Dec2HexTFunction()
            : base("Dec2Hex", TexlStrings.AboutDec2HexT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 2, DType.EmptyTable, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Dec2HexTArg1, TexlStrings.Dec2HexTArg2 };
        }
    }
}