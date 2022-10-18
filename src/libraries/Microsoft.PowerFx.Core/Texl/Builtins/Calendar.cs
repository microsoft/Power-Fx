// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class CalendarFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public CalendarFunction(TexlFunctionConfig instanceConfig, string functionInvariantName, TexlStrings.StringGetter functionDescription)
            : base(instanceConfig, new DPath().Append(new DName(LanguageConstants.InvariantCalendarNamespace)), functionInvariantName, functionDescription, FunctionCategories.DateTime, DType.CreateTable(new TypedName(DType.String, new DName("Value"))), 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }

    // Calendar.MonthsLong()
    internal sealed class MonthsLongFunction : CalendarFunction
    {
        public MonthsLongFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "MonthsLong", TexlStrings.AboutCalendar__MonthsLong)
        {
        }
    }

    // Calendar.MonthsShort()
    internal sealed class MonthsShortFunction : CalendarFunction
    {
        public MonthsShortFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "MonthsShort", TexlStrings.AboutCalendar__MonthsShort)
        {
        }
    }

    // Calendar.WeekdaysLong()
    internal sealed class WeekdaysLongFunction : CalendarFunction
    {
        public WeekdaysLongFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "WeekdaysLong", TexlStrings.AboutCalendar__WeekdaysLong)
        {
        }
    }

    // Calendar.WeekdaysShort()
    internal sealed class WeekdaysShortFunction : CalendarFunction
    {
        public WeekdaysShortFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "WeekdaysShort", TexlStrings.AboutCalendar__WeekdaysShort)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
