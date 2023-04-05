// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Log(number:n, [base:n]):n
    // Equivalent Excel function: Log
    internal sealed class LogFunction : MathTwoArgFunction
    {
        public override bool HasPreciseErrors => true;

        public LogFunction()
            : base("Log", TexlStrings.AboutLog, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
            yield return new[] { TexlStrings.MathFuncArg1, TexlStrings.LogBase };
        }
    }

    // Log(number:n|*[n], [base:n|*[n]]):*[n]
    // Equivalent Excel function: Log
    internal sealed class LogTFunction : MathTwoArgTableFunction
    {
        protected override bool InConsistentTableResultUseSecondArg => true;

        public LogTFunction()
            : base("Log", TexlStrings.AboutLogT, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathTFuncArg1 };
            yield return new[] { TexlStrings.MathTFuncArg1, TexlStrings.LogBase };
        }
    }
}
