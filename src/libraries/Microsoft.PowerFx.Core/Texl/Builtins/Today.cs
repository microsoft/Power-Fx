// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Today()
    // Equivalent DAX/Excel function: Today
    internal sealed class TodayFunction : BuiltinFunction
    {
        // Multiple invocations may result in different return values.
        public override bool IsStateless => false;

        public override bool IsGlobalReliant => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public TodayFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Today", TexlStrings.AboutToday, FunctionCategories.DateTime, DType.Date, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
