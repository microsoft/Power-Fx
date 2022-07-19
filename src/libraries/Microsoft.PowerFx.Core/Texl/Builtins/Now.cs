// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Now()
    // Equivalent DAX/Excel function: Now
    internal sealed class NowFunction : BuiltinFunction
    {
        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;

        public override bool IsGlobalReliant => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public NowFunction()
            : base("Now", TexlStrings.AboutNow, FunctionCategories.DateTime, DType.DateTime, 0, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
