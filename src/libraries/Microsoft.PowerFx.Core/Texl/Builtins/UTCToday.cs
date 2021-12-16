// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // UTCToday()
    internal sealed class UTCTodayFunction : BuiltinFunction
    {
        // Multiple invocations may result in different return values.
        public override bool IsStateless => false;
        public override bool IsGlobalReliant => true;
        public override bool IsSelfContained => true;

        public UTCTodayFunction()
            : base("UTCToday", TexlStrings.AboutUTCToday, FunctionCategories.DateTime, DType.Date, 0, 0, 0)
        { }
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
