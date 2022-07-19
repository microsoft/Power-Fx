// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // UTCNow()
    internal sealed class UTCNowFunction : BuiltinFunction
    {
        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;

        public override bool IsGlobalReliant => true;

        public override bool IsSelfContained => true;

        public UTCNowFunction()
            : base("UTCNow", TexlStrings.AboutUTCNow, FunctionCategories.DateTime, DType.DateTime, 0, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
