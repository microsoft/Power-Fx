// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Pi()
    // Equivalent Excel function: PI
    internal sealed class PiFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public PiFunction()
            : base(
                "Pi",
                TexlStrings.AboutPi,
                FunctionCategories.MathAndStat,
                DType.Number, // return type
                0,            // no lambdas
                0,            // min arity of 0
                0)            // max arity of 0
        { }
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }
}
