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
    // DEC2HEX(number:n, [places:n])
    internal sealed class DEC2HEXFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public DEC2HEXFunction()
            : base("DEC2HEX", TexlStrings.AboutDEC2HEX, FunctionCategories.MathAndStat, DType.String, 0, 1, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DEC2HEXArg1, TexlStrings.DEC2HEXArg2 };
        }
    }

    // DEC2HEXT(number:[n], [places:n])
    internal sealed class DEC2HEXTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public DEC2HEXTableFunction()
            : base("DEC2HEXT", TexlStrings.AboutDEC2HEXT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DEC2HEXTArg1 };
        }

    }
}
