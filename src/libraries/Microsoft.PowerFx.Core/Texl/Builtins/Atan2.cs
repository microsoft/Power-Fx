// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Atan2(number:x, number:y)
    // Equivalent Excel function: Atan2
    internal sealed class Atan2Function : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public Atan2Function()
            : base(
                "Atan2",
                TexlStrings.AboutAtan2,
                FunctionCategories.MathAndStat,
                DType.Number, // return type
                0,            // no lambdas
                2,            // min arity of 2
                2,            // max arity of 2
                DType.Number, // first param is numeric
                DType.Number) // second param is numeric
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutAtan2Arg1, TexlStrings.AboutAtan2Arg2 };
        }

        public override bool SupportsParamCoercion => true;
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
