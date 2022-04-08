// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // TimeZoneOffset()
    internal sealed class TimeZoneOffsetFunction : BuiltinFunction
    {
        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public TimeZoneOffsetFunction()
            : base("TimeZoneOffset", TexlStrings.AboutTimeZoneOffset, FunctionCategories.DateTime, DType.Number, 0, 0, 1, DType.DateTime)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
            yield return new[] { TexlStrings.TimeZoneOffsetArg1 };
        }
    }
}
