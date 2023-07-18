// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.PowerFx.Functions
{
    internal static class FormattingInfoHelper
    {
        internal static FormattingInfo CreateFormattingInfo() => new FormattingInfo(CultureInfo.InvariantCulture, TimeZoneInfo.Local);

        internal static FormattingInfo GetFormattingInfo(this IServiceProvider serviceProvider) => new FormattingInfo(serviceProvider.GetService<CultureInfo>(), serviceProvider.GetService<TimeZoneInfo>());

        internal static FormattingInfo GetFormattingInfo(this EvalVisitor runner) => new FormattingInfo(runner.CultureInfo, runner.TimeZoneInfo);

        internal static FormattingInfo With(this FormattingInfo formatInfo, CultureInfo cultureInfo) => new FormattingInfo(cultureInfo, formatInfo.TimeZoneInfo);
    }
}
