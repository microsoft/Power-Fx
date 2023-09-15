// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsNumeric(expression:E)
    internal sealed class IsTypeFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public IsTypeFunction()
            : base("IsType", TexlStrings.AboutIsType, FunctionCategories.Information, DType.Boolean, 0, 2, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsTypeArg1, TexlStrings.IsTypeArg2 };
        }
    }
}
