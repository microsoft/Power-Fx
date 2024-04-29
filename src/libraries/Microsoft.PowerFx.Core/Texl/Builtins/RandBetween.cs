// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // RandBetween()
    // Equivalent DAX/Excel function: RandBetween
    internal sealed class RandBetweenFunction : MathTwoArgFunction
    {
        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;

        public RandBetweenFunction()
            : base("RandBetween", TexlStrings.AboutRandBetween, minArity: 2, nativeDecimalArgs: 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RandBetweenArg1, TexlStrings.RandBetweenArg2 };
        }
    }
}
