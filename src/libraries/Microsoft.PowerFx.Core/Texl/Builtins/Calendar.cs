// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class CalendarFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public CalendarFunction(string functionInvariantName, TexlStrings.StringGetter functionDescription)
            : base(new DPath().Append(new DName(LanguageConstants.InvariantCalendarNamespace)), functionInvariantName, functionDescription, FunctionCategories.DateTime, DType.CreateTable(new TypedName(DType.String, new DName("Value"))), 0, 0, 0)
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
        public MonthsLongFunction()
            : base("MonthsLong", TexlStrings.AboutCalendar__MonthsLong)
        {
        }
    }

    // Calendar.MonthsShort()
    internal sealed class MonthsShortFunction : CalendarFunction
    {
        public MonthsShortFunction()
            : base("MonthsShort", TexlStrings.AboutCalendar__MonthsShort)
        {
        }
    }

    // Calendar.WeekdaysLong()
    internal sealed class WeekdaysLongFunction : CalendarFunction
    {
        public WeekdaysLongFunction()
            : base("WeekdaysLong", TexlStrings.AboutCalendar__WeekdaysLong)
        {
        }
    }

    // Calendar.WeekdaysShort()
    internal sealed class WeekdaysShortFunction : CalendarFunction
    {
        public WeekdaysShortFunction()
            : base("WeekdaysShort", TexlStrings.AboutCalendar__WeekdaysShort)
        {
        }
    }
}