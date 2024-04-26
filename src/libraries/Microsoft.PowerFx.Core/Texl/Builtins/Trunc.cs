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
    // Trunc(number:n, digits:n)
    // Truncate by rounding toward zero.
    internal sealed class TruncFunction : MathTwoArgFunction
    {
        public TruncFunction()
            : base("Trunc", TexlStrings.AboutTrunc, minArity: 1, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TruncArg1 };
            yield return new[] { TexlStrings.TruncArg1, TexlStrings.TruncArg2 };
        }
    }

    internal sealed class TruncTableFunction : MathTwoArgTableFunction
    {
        public TruncTableFunction()
            : base("Trunc", TexlStrings.AboutTruncT, minArity: 1, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TruncTArg1 };
            yield return new[] { TexlStrings.TruncTArg1, TexlStrings.TruncTArg2 };
        }
    }
}
