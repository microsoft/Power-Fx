// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsNumeric(expression:E)
    internal sealed class IsNumericFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => false;

        public IsNumericFunction()
            : base("IsNumeric", TexlStrings.AboutIsNumeric, FunctionCategories.Information, DType.Boolean, 0, 1, 1)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsNumericArg1 };
        }
    }
}
