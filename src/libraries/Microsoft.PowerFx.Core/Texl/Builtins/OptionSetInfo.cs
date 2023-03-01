// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class OptionSetInfoFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public OptionSetInfoFunction()
            : base("OptionSetInfo", TexlStrings.AboutOptionSetInfo, FunctionCategories.Text, DType.String, 0, 1, 1, DType.OptionSetValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutOptionSetInfoArg1 };
        }
    }
}
