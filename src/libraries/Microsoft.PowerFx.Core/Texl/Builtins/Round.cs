// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
    // Round(number:n, digits:n)
    internal sealed class RoundScalarFunction : MathTwoArgFunction
    {
        public RoundScalarFunction()
            : base("Round", TexlStrings.AboutRound, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundArg1, TexlStrings.RoundArg2 };
        }
    }

    // RoundUp(number:n, digits:n)
    internal sealed class RoundUpScalarFunction : MathTwoArgFunction
    {
        public RoundUpScalarFunction()
            : base("RoundUp", TexlStrings.AboutRoundUp, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundArg1, TexlStrings.RoundArg2 };
        }
    }

    // RoundDown(number:n, digits:n)
    internal sealed class RoundDownScalarFunction : MathTwoArgFunction
    {
        public RoundDownScalarFunction()
            : base("RoundDown", TexlStrings.AboutRoundDown, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundArg1, TexlStrings.RoundArg2 };
        }
    }

    // Round(number:n|*[n], digits:n|*[n])
    internal sealed class RoundTableFunction : MathTwoArgTableFunction
    {
        public RoundTableFunction()
            : base("Round", TexlStrings.AboutRoundT, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundTArg1, TexlStrings.RoundTArg2 };
        }
    }

    // RoundUp(number:n|*[n], digits:n|*[n])
    internal sealed class RoundUpTableFunction : MathTwoArgTableFunction
    {
        public RoundUpTableFunction()
            : base("RoundUp", TexlStrings.AboutRoundUpT, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundTArg1, TexlStrings.RoundTArg2 };
        }
    }

    // RoundDown(number:n|*[n], digits:n|*[n])
    internal sealed class RoundDownTableFunction : MathTwoArgTableFunction
    {
        public RoundDownTableFunction()
            : base("RoundDown", TexlStrings.AboutRoundDownT, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundTArg1, TexlStrings.RoundTArg2 };
        }
    }
}
